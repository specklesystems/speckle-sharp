#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.DoubleNumerics;
using ConverterRevitShared.Revit;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Objects.Other;
using Serilog.Core;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.netDxf.Blocks;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject MappedBlockWrapperToNative(MappedBlockWrapper blockWrapper)
  {
    blockWrapper.instance.typedDefinition["category"] = blockWrapper.category;
    if (blockWrapper.nameOverride != null)
      blockWrapper.instance.typedDefinition.name = blockWrapper.nameOverride;
    return BlockInstanceToNative(blockWrapper.instance);
  }

  public ApplicationObject BlockInstanceToNative(BlockInstance instance)
  {
    var docObj = GetExistingElementByApplicationId(instance.applicationId);
    var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(docObj, appObj))
      return appObj;

    var isUpdate = false;
    if (docObj != null && ReceiveMode == ReceiveMode.Update)
    {
      try
      {
        isUpdate = true;
        Doc.Delete(docObj.Id);
      }
      catch (Exception e)
      {
        SpeckleLog.Logger.Warning(
          e,
          "Unexpected issue when deleting existing BlockInstance, proceeding with new Block creation."
        );
      }
    }

    try
    {
      var familyInstance = ConvertBlockInstanceToFamilyInstance(instance);
      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(
        status: state,
        createdId: familyInstance.UniqueId,
        convertedItem: familyInstance,
        logItem: $"Assigned name: {familyInstance.Symbol.Name}"
      );
    }
    catch (Exception e)
    {
      var errMsg = "An unexpected error occured when converting a BlockInstance to Native";
      SpeckleLog.Logger.Error(e, errMsg);
      appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{errMsg}: {e.ToFormattedString()}");
    }

    return appObj;
  }

  private DB.FamilyInstance ConvertBlockInstanceToFamilyInstance(BlockInstance instance)
  {
    using var revitTransform = TransformToNative(instance.transform);
    instance.transform.Decompose(out var s, out var r, out var t);

    var axisMirrorCheck = (s.X < 0, s.Y < 0, s.Z < 0);
    using DB.Plane pln = GetLocationPlaneForTransform(revitTransform);

    // Get the symbol for this instance's block definition
    var symbol = ConvertBlockDefinitionToFamilySymbol(instance.typedDefinition);

    // Create instance with the correct mode depending if it's nested or not.
    // For now we usually call this at the model level only
    var refPlane = Doc.Create.NewReferencePlane2(
      pln.Origin,
      pln.Origin + pln.XVec,
      pln.Origin + pln.YVec,
      Doc.ActiveView
    );

    var familyInstance = Doc.Create.NewFamilyInstance(refPlane.GetReference(), pln.Origin, pln.XVec, symbol);
    ApplyMirroringToElement(familyInstance.Id, pln, axisMirrorCheck);

    return familyInstance;
  }

  private static DB.Plane GetLocationPlaneForTransform(DB.Transform transform)
  {
    // Get the position of the instance in the current document
    var position = transform.OfPoint(DB.XYZ.Zero);
    // Apply XY mirroring to instance
    var instanceXAxis = transform.OfVector(DB.XYZ.BasisX);
    var instanceYAxis = transform.OfVector(DB.XYZ.BasisY);
    return DB.Plane.CreateByOriginAndBasis(position, instanceXAxis, instanceYAxis);
  }

  private void ApplyMirroringToElement(
    DB.ElementId familyInstanceId,
    DB.Plane plane,
    (bool IsMirorredX, bool IsMirroredY, bool IsMirroredZ) mirrorCheck
  )
  {
    new List<(string name, bool shouldMirror, DB.Plane mirrorPlane)>
    {
      ("YZ", mirrorCheck.IsMirorredX, DB.Plane.CreateByOriginAndBasis(plane.Origin, plane.YVec, plane.Normal)),
      ("XZ", mirrorCheck.IsMirroredY, DB.Plane.CreateByOriginAndBasis(plane.Origin, plane.XVec, plane.Normal)),
      ("XY", mirrorCheck.IsMirroredZ, DB.Plane.CreateByOriginAndBasis(plane.Origin, plane.XVec, plane.YVec))
    }
      .Where(i => i.shouldMirror)
      .ToList()
      .ForEach(item =>
      {
        try
        {
          Doc.Regenerate();
          DB.ElementTransformUtils.MirrorElements(
            Doc,
            new List<DB.ElementId> { familyInstanceId },
            item.mirrorPlane,
            false
          );
        }
        catch (Exception e)
        {
          SpeckleLog.Logger.Warning(e, "Failed to mirror element on {name} plane", item.name);
        }
      });
  }

  private DB.FamilySymbol ConvertBlockDefinitionToFamilySymbol(BlockDefinition definition)
  {
    // TODO: Geometry update of existing family is not implemented yet.
    using var family = FindBlockDefinitionFamily(definition) ?? CreateBlockDefinitionFamily(definition);

    // TODO: We're still picking the first one here. New symbol creation for other scales is not yet supported
    var symbol =
      FindBlockDefinitionFamilySymbol(definition, family) ?? CreateBlockDefinitionFamilySymbol(definition, family);

    return symbol;
  }

  private DB.Family CreateBlockDefinitionFamily(BlockDefinition definition)
  {
    var famDoc = CreateNewFamilyTemplateDoc();

    // Get the flat list of geometry and scale using transform to get it to the right size.
    var flatGeometry = GetBlockDefinitionGeometry(definition).Select(bt => bt as Base);

    // Grab all geometry from the definition, convert and add to family symbol.
    PopulateFamilyWithBlockDefinitionGeometry(famDoc, flatGeometry);
    // Change the category of this definition
    AssignCategoryToFamilyDoc(famDoc, definition["category"] as string);

    string tempFamilyPath = Path.Combine(Path.GetTempPath(), GetFamilyNameFor(definition) + ".rfa");
    using var so = new DB.SaveAsOptions();
    so.OverwriteExistingFile = true;

    // Save and close the doc
    famDoc.SaveAs(tempFamilyPath, so);
    famDoc.Close();

    // Load the new document into the model doc
    Doc.LoadFamily(tempFamilyPath, new FamilyLoadOption(), out var family);
    return family;
  }

  private static void AssignCategoryToFamilyDoc(DB.Document famDoc, string? categoryName)
  {
    // Get the RevitCategory from a string value
    var success = Enum.TryParse(categoryName, out RevitCategory cat);
    if (!success)
      cat = RevitCategory.GenericModel;

    // Get the BuiltInCategory corresponding to the RevitCategory
    var catName = Categories.GetBuiltInFromSchemaBuilderCategory(cat);
    success = Enum.TryParse(catName, out DB.BuiltInCategory bic);
    if (!success)
      bic = DB.BuiltInCategory.OST_GenericModel;

    // Get the actual category from the document
    DB.Category familyCategory = famDoc.Settings.Categories.get_Item(bic);

    using var t = new DB.Transaction(famDoc, "Change family category");

    try
    {
      t.Start();
      // Swap the family category for the one we want
      famDoc.OwnerFamily.FamilyCategory = familyCategory;
      t.Commit();
    }
    catch (Exception e)
    {
      SpeckleLog.Logger.Warning(e, "Document category could not be modified");
      t.RollBack();
    }
  }

  /// <summary>
  /// Re-creates the BlockDefinition geometry into the provided document.
  /// This will return every geometry (including nested families) at the top-level.
  /// Creation of sub-families is currently not supported.
  /// </summary>
  /// <param name="definition">The BlockDefinition to extract the geometry from</param>
  /// <param name="famDoc">The revit document where the geometry will be added</param>
  private static void PopulateFamilyWithBlockDefinitionGeometry(DB.Document famDoc, IEnumerable<Base?> geometry)
  {
    // Start up a local converter to isolate conversions for this particular family document.
    // This prevents other conversions from being polluted by multi-document environments.
    var converter = new ConverterRevit();
    converter.SetContextDocument(famDoc); // Always remember to set the doc 🙂

    // Using the family document, add all geometry of the instance at the root level (no nesting)
    using DB.Transaction t = new(famDoc, $"Create geometry for block definition");
    t.Start();
    foreach (var o in geometry)
      converter.ConvertToNativeObject(o);
    t.Commit();
  }

  private DB.FamilySymbol? FindBlockDefinitionFamilySymbol(BlockDefinition definition, DB.Family family)
  {
    // The loaded family contains all the possible symbols (variations)
    // We must pick the right one and return.
    // TODO: For now, we're just picking the first.
    var element = Doc.GetElement(family.GetFamilySymbolIds().First());
    if (element is not DB.FamilySymbol symbol)
      return null;
    if (!symbol.IsActive)
      symbol.Activate();
    return symbol;
  }

  private DB.FamilySymbol CreateBlockDefinitionFamilySymbol(BlockDefinition definition, DB.Family family)
  {
    throw new NotImplementedException("Creating family symbols is not supported yet");
  }

  /// <summary>
  ///   Creates a new family document as a duplicate of the specified template.
  /// </summary>
  /// <param name="name">The name of the template to use to create the family document.</param>
  /// <returns>A new family document based based on the specified template.</returns>
  /// <exception cref="System.IO.FileNotFoundException">When the file corresponding to the provided template name could not be found</exception>
  /// <exception cref="Autodesk.Revit.Exceptions.InvalidOperationException">When the document could not be opened</exception>
  private DB.Document CreateNewFamilyTemplateDoc(string name = "Block")
  {
    var templatePath = GetTemplatePath(name);
    if (!File.Exists(templatePath))
      throw new FileNotFoundException($"Could not find '{name}.rft' template file - {templatePath}");
    return Doc.Application.NewFamilyDocument(templatePath);
  }

  /// <summary>
  /// Returns the <see cref="DB.Family"/> instance for a given <see cref="BlockDefinition"/> if it exists.
  /// The family is found by looking up on the current context document for any family that
  /// matches the BlockDefinition name.
  /// </summary>
  /// <param name="definition">The <see cref="BlockDefinition"/> to find a Family for.</param>
  /// <returns>The <see cref="DB.Family"/> instance if it was found, null otherwise.</returns>
  private DB.Family? FindBlockDefinitionFamily(BlockDefinition definition)
  {
    return FindFamilyByName(Doc, GetFamilyNameFor(definition));
  }

  /// <summary>
  /// Attempts to find a <see cref="DB.Family"/> instance in the given document.
  /// This will only succeed if it finds an exact match.
  /// </summary>
  /// <param name="doc">The document to search in.</param>
  /// <param name="familyName">The name of the family to search for.</param>
  /// <returns>The <see cref="DB.Family"/> instance if found, null otherwise.</returns>
  private static DB.Family? FindFamilyByName(DB.Document doc, string familyName)
  {
    // Filter for family elements
    using DB.FilteredElementCollector collector = new(doc);
    var families = collector.OfClass(typeof(DB.Family)).ToElements();

    foreach (DB.Element elem in families)
      if (elem is DB.Family fam && fam.Name == familyName)
        return fam; // Cast to Family and return if the names match

    return null; // Return null if not found
  }

  /// <summary>
  /// Utility function to consolidate the naming pattern for <see cref="BlockDefinition"/> instances in Revit.
  /// The current pattern will return the name of the block, followed by the prefix '_SpeckleBlock'
  /// </summary>
  /// <param name="definition">The block definition we want to extract the family name from.</param>
  /// <returns>The corresponding family name in Revit for the given block definition.</returns>
  /// <remarks>This is done to prevent naming conflicts with existing families in the document.</remarks>
  /// <example>For a block named "Chair", it's Revit family name would be "Chair_SpeckleBlock"</example>
  private static string GetFamilyNameFor(BlockDefinition definition)
  {
    return definition.name + "_SpeckleBlock";
  }

  /// <summary>
  /// Gets the geometric representation of a given block definition. This will 'flatten' all inner instances so that
  /// all that remains is the geometric entities, properly translated into this <see cref="BlockDefinition"/>'s transform space.
  /// </summary>
  /// <param name="definition">The definition to extract</param>
  /// <param name="scaleTransform"></param>
  /// <returns></returns>
  private static IEnumerable<ITransformable?> GetBlockDefinitionGeometry(BlockDefinition definition)
  {
    return definition.geometry
      .SelectMany(
        b =>
          b switch
          {
            // This cast to Base is safe. Compiler just can't safely know ITransformable is only applied to Base objects.
            Instance i => i.GetTransformedGeometry(), // Flattening inner instances here.
            ITransformable bt => new List<ITransformable> { bt },
            _
              => (b.GetDetachedProp("displayValue") as IList)?.OfType<ITransformable>()
                ?? Enumerable.Empty<ITransformable>()
          }
      )
      .Where(bt => bt != null);
  }
}

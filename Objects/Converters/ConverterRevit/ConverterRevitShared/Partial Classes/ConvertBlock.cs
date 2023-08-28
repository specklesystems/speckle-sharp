#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.DoubleNumerics;
using ConverterRevitShared.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Objects.Other;
using Speckle.Core.Logging;
using Speckle.netDxf.Blocks;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject BlockInstanceToNative(BlockInstance instance)
  {
    var docObj = GetExistingElementByApplicationId(instance.applicationId);
    var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(docObj, appObj))
      return appObj;

    var isUpdate = false;
    if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Update)
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
    instance.transform.Decompose(out var scale, out _, out _);
    var scaleTransform = new Transform { matrix = GetScaleMatrix(scale), units = instance.transform.units };

    var symbol = ConvertBlockDefinitionToFamilySymbol(instance.typedDefinition, scaleTransform);

    // Get the position of the instance in the current document
    using var revitTransform = TransformToNative(instance.transform);
    var position = revitTransform.OfPoint(DB.XYZ.Zero);

    // Create instance with the correct mode depending if it's nested or not.
    // TODO: This is just prep for deep nesting support. For now we usually call this at the model level only.
    var structuralType = DB.Structure.StructuralType.NonStructural;
    var familyInstance = Doc.IsFamilyDocument
      ? Doc.FamilyCreate.NewFamilyInstance(position, symbol, structuralType)
      : Doc.Create.NewFamilyInstance(position, symbol, structuralType);

    return familyInstance;
  }

  private DB.FamilySymbol ConvertBlockDefinitionToFamilySymbol(BlockDefinition definition, Transform transform)
  {
    using var family = FindBlockDefinitionFamily(definition) ?? CreateBlockDefinitionFamily(definition, transform);

    // TODO: We're still picking the first one here. New symbol creation for other scales is not yet supported
    var symbol =
      FindBlockDefinitionFamilySymbol(definition, family) ?? CreateBlockDefinitionFamilySymbol(definition, family);
    return symbol;
  }

  private DB.Family CreateBlockDefinitionFamily(BlockDefinition definition, Transform transform)
  {
    var famDoc = CreateNewFamilyTemplateDoc();
    // Grab all geometry from the definition, convert and add to family symbol.
    PopulateFamilyWithBlockDefinitionGeometry(definition, famDoc, transform);

    // Load the new document into the model doc
    var famName = GetFamilyNameFor(definition);

    //using var t = new DB.Transaction(famDoc, "Update document info");
    string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
    using var so = new DB.SaveAsOptions();
    so.OverwriteExistingFile = true;
    famDoc.SaveAs(tempFamilyPath, so);
    famDoc.Close();
    Doc.LoadFamily(tempFamilyPath, new FamilyLoadOption(), out var family);
    return family;
  }

  /// <summary>
  /// Re-creates the BlockDefinition geometry into the provided document.
  /// This will return every geometry (including nested families) at the top-level.
  /// Creation of sub-families is currently not supported.
  /// </summary>
  /// <param name="definition">The BlockDefinition to extract the geometry from</param>
  /// <param name="famDoc">The revit document where the geometry will be added</param>
  private void PopulateFamilyWithBlockDefinitionGeometry(
    BlockDefinition definition,
    DB.Document famDoc,
    Transform scaleTransform
  )
  {
    // Get the flat list of geometry
    var flatGeometry = GetBlockDefinitionGeometry(definition)
      .Select(bt =>
      {
        bt.TransformTo(scaleTransform, out var transformed);
        return transformed as Base;
      });

    // Start up a local converter to isolate conversions for this particular family document.
    // This prevents other conversions from being polluted by multi-document environments.
    var converter = new ConverterRevit();
    converter.SetContextDocument(famDoc); // Always remember to set the doc 🙂

    // Using the family document, add all geometry of the instance at the root level (no nesting)
    using DB.Transaction t = new(famDoc, $"Create geometry for block definition - {definition.id}");
    t.Start();
    foreach (var o in flatGeometry)
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
      throw new Exception($"Could not find any symbols in family {family.Name}");
    if (!symbol.IsActive)
      symbol.Activate();
    symbol.Name = "Default";
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
  private DB.Document CreateNewFamilyTemplateDoc(string name = "Generic Model")
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
  /// Creates a new transform that contains scaling values exclusively.
  /// </summary>
  /// <param name="scale">The scale vector to use in the transform.</param>
  /// <returns>The resulting scaling transform.</returns>
  private static Matrix4x4 GetScaleMatrix(Vector3 scale)
  {
    var matrix = new Matrix4x4(scale.X, 0, 0, 0, 0, scale.Y, 0, 0, 0, 0, scale.Z, 0, 0, 0, 0, 1);
    return matrix;
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
            _ => null
          }
      )
      .Where(bt => bt != null);
  }
}

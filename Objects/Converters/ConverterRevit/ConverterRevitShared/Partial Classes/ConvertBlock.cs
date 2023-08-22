#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Objects.Other;
using Speckle.Core.Logging;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject? BlockInstanceToNative(BlockInstance instance)
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
      appObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"BlockInstance could not be created: {e.ToFormattedString()}"
      );
    }

    return appObj;
  }

  private DB.FamilyInstance ConvertBlockInstanceToFamilyInstance(BlockInstance instance)
  {
    var symbol = ConvertBlockDefinitionToFamilySymbol(instance.typedDefinition);
    instance.transform.Decompose(out var scale, out var rot, out var trans);

    var position = new DB.XYZ(trans.X / trans.W, trans.Y / trans.W, trans.Z / trans.W);

    // Create instance with the correct mode depending if it's nested or not.
    // TODO: This is just prep for deep nesting support. For now we usually call this at the model level only.
    var structuralType = DB.Structure.StructuralType.NonStructural;
    var familyInstance = Doc.IsFamilyDocument
      ? Doc.FamilyCreate.NewFamilyInstance(position, symbol, structuralType)
      : Doc.Create.NewFamilyInstance(position, symbol, structuralType);

    RotateFamilyInstance(familyInstance, rot.Z / rot.W);

    return familyInstance;
  }

  private void RotateFamilyInstance(DB.FamilyInstance familyInstance, double angle)
  {
    DB.Transform t = familyInstance.GetTotalTransform();
    using DB.Line zLine = DB.Line.CreateUnbound(t.Origin, t.BasisZ);
    DB.ElementTransformUtils.RotateElement(Doc, familyInstance.Id, zLine, angle);
  }

  private DB.FamilySymbol ConvertBlockDefinitionToFamilySymbol(BlockDefinition definition)
  {
    // TODO: Update behaviour should skip the family creation and spit out the symbol to be reused
    var famDoc = CreateNewFamilyTemplateDoc();
    PopulateFamilyWithBlockDefinitionGeometry(definition, famDoc);
    // Load the new document into the model doc
    var family = Doc.LoadFamily(famDoc);
    var symbol = GetFamilySymbolForBlockDefinition(family);
    return symbol;
  }

  private void PopulateFamilyWithBlockDefinitionGeometry(BlockDefinition definition, DB.Document famDoc)
  {
    // Get the flat list of geometry
    var flatGeometry = definition.geometry
      .SelectMany(
        b =>
          b switch
          {
            Instance i => i.GetTransformedGeometry().Cast<Base>().ToList(),
            // This cast to Base is safe. Compiler just can't safely know ITransformable is only applied to Base objects.
            ITransformable bt => new List<Base> { (bt as Base)! },
            _ => null
          }
      )
      .ToList();

    // Start up a local converter to isolate conversions for this particular family document.
    // This prevents other conversions from being polluted by multi-document environments.
    var converter = new ConverterRevit();
    converter.SetContextDocument(famDoc); // Always remember to set the doc 🙂

    // Using the family document, add all geometry of the instance at the root level (no nesting)
    using DB.Transaction t = new(famDoc, $"Create geometry for block definition - {definition.id}");
    t.Start();
    flatGeometry.ForEach(o => converter.ConvertToNative(o));
    t.Commit();
  }

  private DB.FamilySymbol GetFamilySymbolForBlockDefinition(DB.Family family)
  {
    // The loaded family contains all the possible symbols (variations)
    // We must pick the right one and return.
    // TODO: For now, we're just picking the first.
    var element = Doc.GetElement(family.GetFamilySymbolIds().First());
    if (element is not DB.FamilySymbol symbol)
      throw new Exception($"Could not find any symbols in family {family.Name}");
    if (!symbol.IsActive)
      symbol.Activate();
    return symbol;
  }

  /// <summary>
  ///   Creates a new family document as a duplicate of the specified template.
  /// </summary>
  /// <param name="name">The name of the template to use to create the family document.</param>
  /// <returns>A new family document based based on the specified template.</returns>
  /// <exception cref="System.IO.FileNotFoundException">When the file corresponding to the provided template name could not be found</exception>
  /// <exception cref="Autodesk.Revit.Exceptions.InvalidOperationException">When the document could not be opened</exception>
  public DB.Document CreateNewFamilyTemplateDoc(string name = "Generic Model")
  {
    var templatePath = GetTemplatePath(name);
    if (!File.Exists(templatePath))
      throw new FileNotFoundException($"Could not find '{name}.rft' template file - {templatePath}");
    return Doc.Application.NewFamilyDocument(templatePath);
  }
}

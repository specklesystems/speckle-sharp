using Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject FreeformElementToNative( Objects.BuiltElements.Revit.FreeformElement freeformElement )
    {
      var appObj = new ApplicationObject(freeformElement.id, freeformElement.speckle_type) { applicationId = freeformElement.applicationId };

      // 1. Convert the freeformElement geometry to native
      var solids = new List<DB.Solid>();
      foreach (var geom in freeformElement.baseGeometries)
        switch (geom)
        {
          case Brep brep:
            try
            {
              var solid = BrepToNative(geom as Brep, out List<string> brepNotes);
              if (brepNotes.Count > 0) appObj.Update(log: brepNotes);
              solids.Add(solid);
            }
            catch (Exception e)
            {
              appObj.Update(logItem: $"Could not convert brep to native, falling back to mesh representation: {e.Message}");
              var brepMeshSolids = GetSolidMeshes(brep.displayValue);
              solids.AddRange(brepMeshSolids);
            }
            break;
          case Objects.Geometry.Mesh mesh:
            var meshSolids = MeshToNative(mesh, DB.TessellatedShapeBuilderTarget.Solid, DB.TessellatedShapeBuilderFallback.Abort)
                .Select(m => m as DB.Solid);
            solids.AddRange(meshSolids);
            break;
        }

      var tempPath = CreateFreeformElementFamily(solids, freeformElement.id, out List<string> notes, freeformElement);
      appObj.Update(log: notes);
      if (tempPath == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }
      Doc.LoadFamily(tempPath, new FamilyLoadOption(), out var fam);
      var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
      symbol.Activate();
      try
      {
        File.Delete(tempPath);
      }
      catch { }

      var freeform = Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);
      appObj.Update(status: ApplicationObject.State.Created, createdId: freeform.UniqueId, convertedItem: freeform);
      SetInstanceParameters(freeform, freeformElement);
      return appObj;
    }

    public ApplicationObject FreeformElementToNativeFamily(Brep brep, Category cat = null)
    {
      var appObj = new ApplicationObject(brep.id, brep.speckle_type) { applicationId = brep.applicationId };
      var solids = new List<DB.Solid>();
      try
      {
        var solid = BrepToNative(brep, out List<string> brepNotes);
        if (brepNotes.Count > 0) appObj.Update(log: brepNotes);
        solids.Add(solid);
      }
      catch (Exception e)
      {
        var meshes = GetSolidMeshes(brep.displayValue);
        solids.AddRange(meshes);
      }

      foreach (var s in solids)
      {
        var form = DB.FreeFormElement.Create(Doc, s);
        if (cat != null)
          form.Subcategory = cat;
        appObj.Update(createdId: form.UniqueId, convertedItem: s);
      }

      return appObj;
    }

    public ApplicationObject FreeformElementToNativeFamily(Geometry.Mesh mesh)
    {
      var appObj = new ApplicationObject(mesh.id, mesh.speckle_type) { applicationId = mesh.applicationId };
      var solids = new List<DB.Solid>();
      var d = MeshToNative(mesh, DB.TessellatedShapeBuilderTarget.Solid);
      var revitMmesh =
          d.Select(m => m as DB.Solid);
      solids.AddRange(revitMmesh);

      foreach (var s in solids)
      {
        var form = DB.FreeFormElement.Create(Doc, s);
        appObj.Update(createdId: form.UniqueId, convertedItem: s);
      }

      return appObj;
    }

    private IEnumerable<Solid> GetSolidMeshes(IEnumerable<Mesh> meshes)
    {
      return meshes
        .SelectMany(m => MeshToNative(m, DB.TessellatedShapeBuilderTarget.Solid, DB.TessellatedShapeBuilderFallback.Abort))
        .Select(m => m as DB.Solid);
    }

    private ApplicationObject FreeformElementToNative(Brep brep)
    {
      var appObj = new ApplicationObject(brep.id, brep.speckle_type) { applicationId = brep.applicationId };
      var solids = new List<DB.Solid>();
      try
      {
        var solid = BrepToNative(brep, out List<string> brepNotes);
        if (brepNotes.Count > 0) appObj.Update(log: brepNotes);
        solids.Add(solid);
      }
      catch (Exception e)
      {
        solids.AddRange(GetSolidMeshes(brep.displayValue));
      }

      var tempPath = CreateFreeformElementFamily(solids, brep.id, out List<string> freeformNotes);
      if (freeformNotes.Count > 0) appObj.Update(log: freeformNotes);
      if (tempPath == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      Doc.LoadFamily(tempPath, new FamilyLoadOption(), out var fam);
      var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
      symbol.Activate();

      var freeform = Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);

      SetInstanceParameters(freeform, brep);
      
      appObj.Update(status: ApplicationObject.State.Created, createdId: freeform.UniqueId, convertedItem: freeform);
      return appObj;
    }

    private string CreateFreeformElementFamily(List<DB.Solid> solids, string name, out List<string> notes, Objects.BuiltElements.Revit.FreeformElement freeformElement = null)
    {
      notes = new List<string>();
      // FreeformElements can only be created in a family context.
      // so we create a temporary family to hold it.

      var templatePath = GetTemplatePath("Generic Model");
      if (!File.Exists(templatePath))
      {
        notes.Add($"Could not find Generic Model rft file - {templatePath}");
        return null;
      }

      var famDoc = Doc.Application.NewFamilyDocument(templatePath);

      using (DB.Transaction t = new DB.Transaction(famDoc, "Create Freeform Elements"))
      {
        t.Start();

        Category cat = null;
        if (freeformElement != null)
        {
          //subcategory
          BuiltInCategory bic;
          if (!string.IsNullOrEmpty(freeformElement.subcategory))
          {
            //by default free form elements are always generic models
            //otherwise we'd need to supply base files for each category..?
            var bicName = Categories.GetBuiltInFromSchemaBuilderCategory(BuiltElements.Revit.RevitCategory.GenericModels);
            BuiltInCategory.TryParse(bicName, out bic);
            cat = famDoc.Settings.Categories.get_Item(bic);
            if (cat.SubCategories.Contains(freeformElement.subcategory))
              cat = cat.SubCategories.get_Item(freeformElement.subcategory);
            else
              cat = famDoc.Settings.Categories.NewSubcategory(cat, freeformElement.subcategory);
          }
        }

        foreach (var s in solids)
        {
          var f = DB.FreeFormElement.Create(famDoc, s);
          f.Subcategory = cat;
        }

        t.Commit();
      }
      var famName = "SpeckleFreeform_" + name;
      string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
      var so = new DB.SaveAsOptions();
      so.OverwriteExistingFile = true;
      famDoc.SaveAs(tempFamilyPath, so);
      famDoc.Close();
      notes.Add($"Created temp family {tempFamilyPath}");
      return tempFamilyPath;
    }
  }
}
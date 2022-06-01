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
    public ApplicationPlaceholderObject FreeformElementToNative(
        Objects.BuiltElements.Revit.FreeformElement freeformElement)
    {
      // 1. Convert the freeformElement geometry to native
      var solids = new List<DB.Solid>();
      foreach (var geom in freeformElement.baseGeometries)
        switch (geom)
        {
          case Brep brep:
            try
            {
              var solid = BrepToNative(geom as Brep);
              solids.Add(solid);
            }
            catch (Exception e)
            {
              Report.LogConversionError(new SpeckleException($"Could not convert BREP {freeformElement.id} to native, falling back to mesh representation.", e));

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




      var tempPath = CreateFreeformElementFamily(solids, freeformElement.id, freeformElement);
      Doc.LoadFamily(tempPath, new FamilyLoadOption(), out var fam);
      var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
      symbol.Activate();
      try
      {
        File.Delete(tempPath);
      }
      catch
      {
      }

      var freeform = Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);

      SetInstanceParameters(freeform, freeformElement);
      Report.Log($"Created FreeformElement {freeform.Id}");
      return new ApplicationPlaceholderObject
      {
        applicationId = freeformElement.id,
        ApplicationGeneratedId = freeform.UniqueId,
        NativeObject = freeform
      };
    }


    public List<ApplicationPlaceholderObject> FreeformElementToNativeFamily(Brep brep, Category cat = null)
    {
      var solids = new List<DB.Solid>();
      try
      {
        var solid = BrepToNative(brep);
        solids.Add(solid);
      }
      catch (Exception e)
      {
        var meshes = GetSolidMeshes(brep.displayValue);
        solids.AddRange(meshes);
      }

      var applicationPlaceholders = new List<ApplicationPlaceholderObject>();

      foreach (var s in solids)
      {
        var form = DB.FreeFormElement.Create(Doc, s);
        if (cat != null)
          form.Subcategory = cat;
        applicationPlaceholders.Add(new ApplicationPlaceholderObject
        {
          ApplicationGeneratedId = form.UniqueId,
          NativeObject = s
        });
        Report.Log($"Created FreeformElement {form.Id}");
      }



      return applicationPlaceholders;


    }

    public List<ApplicationPlaceholderObject> FreeformElementToNativeFamily(Geometry.Mesh mesh)
    {
      var solids = new List<DB.Solid>();
      var d = MeshToNative(mesh, DB.TessellatedShapeBuilderTarget.Solid);
      var revitMmesh =
          d.Select(m => (m as DB.Solid));
      solids.AddRange(revitMmesh);


      var applicationPlaceholders = new List<ApplicationPlaceholderObject>();

      foreach (var s in solids)
      {
        var form = DB.FreeFormElement.Create(Doc, s);

        applicationPlaceholders.Add(new ApplicationPlaceholderObject
        {
          ApplicationGeneratedId = form.UniqueId,
          NativeObject = s
        });
        Report.Log($"Created FreeformElement {form.Id}");
      }

      return applicationPlaceholders;


    }

    private IEnumerable<Solid> GetSolidMeshes(IEnumerable<Mesh> meshes)
    {
      return meshes
        .SelectMany(m => MeshToNative(m, DB.TessellatedShapeBuilderTarget.Solid, DB.TessellatedShapeBuilderFallback.Abort))
        .Select(m => m as DB.Solid);
    }

    private ApplicationPlaceholderObject FreeformElementToNative(Brep brep)
    {
      var solids = new List<DB.Solid>();
      try
      {
        var solid = BrepToNative(brep);
        solids.Add(solid);
      }
      catch (Exception e)
      {
        solids.AddRange(GetSolidMeshes(brep.displayValue));
      }

      var tempPath = CreateFreeformElementFamily(solids, brep.id);
      Doc.LoadFamily(tempPath, new FamilyLoadOption(), out var fam);
      var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
      symbol.Activate();


      var freeform = Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);

      SetInstanceParameters(freeform, brep);
      Report.Log($"Created FreeformElement {freeform.Id}");
      return new ApplicationPlaceholderObject
      {
        applicationId = brep.applicationId,
        ApplicationGeneratedId = freeform.UniqueId,
        NativeObject = freeform
      };
    }

    private string CreateFreeformElementFamily(List<DB.Solid> solids, string name, Objects.BuiltElements.Revit.FreeformElement freeformElement = null)
    {
      // FreeformElements can only be created in a family context.
      // so we create a temporary family to hold it.

      var templatePath = GetTemplatePath("Generic Model");
      if (!File.Exists(templatePath))
      {
        throw new Exception($"Could not find Generic Model rft file - {templatePath}");
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
            {
              cat = cat.SubCategories.get_Item(freeformElement.subcategory);
            }
            else
            {
              cat = famDoc.Settings.Categories.NewSubcategory(cat, freeformElement.subcategory);
            }
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
      Report.Log($"Created temp family {tempFamilyPath}");
      return tempFamilyPath;
    }
  }
}
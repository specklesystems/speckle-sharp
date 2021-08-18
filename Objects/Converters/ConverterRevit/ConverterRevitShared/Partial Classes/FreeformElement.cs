using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationPlaceholderObject FreeformElementToNative(
        Objects.BuiltElements.Revit.FreeformElement freeformElement)
    {
      // 1. Convert the freeformElement geometry to native
      var solids = new List<DB.Solid>();
      switch (freeformElement.baseGeometry)
      {
        case Brep brep:
          try
          {
            var solid = BrepToNative(freeformElement.baseGeometry as Brep);
            solids.Add(solid);
          }
          catch (Exception e)
          {
            ConversionErrors.Add(new SpeckleException($"Could not convert BREP {freeformElement.id} to native, falling back to mesh representation.", e));
            var brepMeshSolids = MeshToNative(brep.displayMesh, DB.TessellatedShapeBuilderTarget.Solid, DB.TessellatedShapeBuilderFallback.Abort)
                .Select(m => m as DB.Solid);
            solids.AddRange(brepMeshSolids);
          }
          break;
        case Objects.Geometry.Mesh mesh:
          var meshSolids = MeshToNative(mesh, DB.TessellatedShapeBuilderTarget.Solid, DB.TessellatedShapeBuilderFallback.Abort)
              .Select(m => m as DB.Solid);
          solids.AddRange(meshSolids);
          break;
      }


      var tempPath = CreateFreeformElementFamily(solids, freeformElement.id);
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
      return new ApplicationPlaceholderObject
      {
        applicationId = freeformElement.id,
        ApplicationGeneratedId = freeform.UniqueId,
        NativeObject = freeform
      };
    }


    public List<ApplicationPlaceholderObject> FreeformElementToNativeFamily(Brep brep)
    {
      var solids = new List<DB.Solid>();
      try
      {
        var solid = BrepToNative(brep);
        solids.Add(solid);
      }
      catch (Exception e)
      {
        var mesh = MeshToNative(brep.displayMesh, DB.TessellatedShapeBuilderTarget.Solid)
            .Select(m => (m as DB.Solid));
        solids.AddRange(mesh);
      }

      var applicationPlaceholders = new List<ApplicationPlaceholderObject>();

      foreach (var s in solids)
      {
        var form = DB.FreeFormElement.Create(Doc, s);
        applicationPlaceholders.Add(new ApplicationPlaceholderObject
        {
          ApplicationGeneratedId = form.UniqueId,
          NativeObject = s
        });
      }



      return applicationPlaceholders;


    }

    public List<ApplicationPlaceholderObject> FreeformElementToNativeFamily(Geometry.Mesh mesh)
    {
      var solids = new List<DB.Solid>();

      var revitMmesh = MeshToNative(mesh, DB.TessellatedShapeBuilderTarget.Solid)
          .Select(m => (m as DB.Solid));
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
      }

      return applicationPlaceholders;


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
        var mesh = MeshToNative(brep.displayMesh, DB.TessellatedShapeBuilderTarget.Solid)
            .Select(m => (m as DB.Solid));
        solids.AddRange(mesh);
      }

      var tempPath = CreateFreeformElementFamily(solids, brep.id);
      Doc.LoadFamily(tempPath, new FamilyLoadOption(), out var fam);
      var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
      symbol.Activate();


      var freeform = Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);
      SetInstanceParameters(freeform, brep);
      return new ApplicationPlaceholderObject
      {
        applicationId = brep.applicationId,
        ApplicationGeneratedId = freeform.UniqueId,
        NativeObject = freeform
      };
    }

    private string CreateFreeformElementFamily(List<DB.Solid> solids, string name)
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

        foreach (var s in solids)
        {
          DB.FreeFormElement.Create(famDoc, s);
        }

        t.Commit();
      }

      var famName = "SpeckleFreeform_" + name;
      string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
      var so = new DB.SaveAsOptions();
      so.OverwriteExistingFile = true;
      famDoc.SaveAs(tempFamilyPath, so);
      famDoc.Close();

      return tempFamilyPath;
    }
  }
}
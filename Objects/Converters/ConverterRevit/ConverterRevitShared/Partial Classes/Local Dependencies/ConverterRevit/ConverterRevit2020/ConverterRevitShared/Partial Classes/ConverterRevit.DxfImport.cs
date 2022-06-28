using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.Converters.DxfConverter;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using FamilyInstance = Objects.BuiltElements.Revit.FamilyInstance;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationPlaceholderObject MeshToDxfImport(Mesh mesh, DB.Document doc)
    {
      var el = CreateDxfImport(new List<Base> { mesh }, $"Speckle-Mesh-{mesh.id}-{mesh.applicationId}.dxf", doc);
      return new ApplicationPlaceholderObject
      {
        id = mesh.id,
        applicationId = mesh.applicationId,
        NativeObject = el,
        ApplicationGeneratedId = el.UniqueId
      };
    }
    
    public ApplicationPlaceholderObject MeshToDxfImportFamily(Mesh mesh, DB.Document doc)
    {
      var el = CreateDxfImportFamily(new List<Base> { mesh }, $"Speckle-Mesh-{mesh.id}-{mesh.applicationId}", doc);
      return new ApplicationPlaceholderObject
      {
        id = mesh.id,
        applicationId = mesh.applicationId,
        NativeObject = el,
        ApplicationGeneratedId = el.UniqueId
      };
    }

    public ApplicationPlaceholderObject BrepToDxfImport(Brep brep, DB.Document doc)
    {
      var el = CreateDxfImport(new List<Base>(brep.displayValue), $"Speckle-Brep-{brep.id}-{brep.applicationId}",
        doc);
      return new ApplicationPlaceholderObject
      {
        id = brep.id,
        applicationId = brep.applicationId,
        NativeObject = el,
        ApplicationGeneratedId = el.UniqueId
      };
    }

    public ApplicationPlaceholderObject BrepToDxfImportFamily(Brep brep, DB.Document doc)
    {
      var el = CreateDxfImportFamily(new List<Base>(brep.displayValue), $"Speckle-Brep-{brep.id}-{brep.applicationId}",
        doc);
      return new ApplicationPlaceholderObject
      {
        id = brep.id,
        applicationId = brep.applicationId,
        NativeObject = el,
        ApplicationGeneratedId = el.UniqueId
      };
    }

    public DB.Element CreateDxfImport(List<Base> objects, string fileName, DB.Document doc)
    {
      var dxfConverter = new SpeckleDxfConverter
      {
        Settings =
        {
          PrettyMeshes = true
        }
      };
      dxfConverter.SetContextDocument(null); // Resets the internal Doc.

      var collection = dxfConverter.ConvertToNative(objects).Cast<Speckle.netDxf.Entities.EntityObject>();

      dxfConverter.Doc.Entities.Add(collection.ToList().Where(x => x != null));

      var folderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Speckle",
        "Temp",
        "Dxf");

      // Ensure directory exists
      if (!Directory.Exists(folderPath))
        Directory.CreateDirectory(folderPath);
      
      // Save the DXF file
      var path = Path.Combine(folderPath, fileName + ".dxf");
      dxfConverter.Doc.Save(path);

      // Create a 3D view to import the SAT file
      var typeId = doc.GetDefaultElementTypeId(DB.ElementTypeGroup.ViewType3D);
      var view = DB.View3D.CreatePerspective(doc, typeId);

      // Call Doc Import
      var success = doc.Import(
        path,
        new DB.DWGImportOptions
        {
          Unit = DB.ImportUnit.Millimeter,
          CustomScale = 1
        },
        view, out var elementId);
      
      doc.Delete(view.Id);
      File.Delete(path);

      if (!success)
        throw new SpeckleException($"Failed to import DXF file: {path}", false);
      
      var el = doc.GetElement(elementId);
      el.Pinned = false;
      return el;
    }
    
    public DB.FamilyInstance CreateDxfImportFamily(List<Base> objects, string fileName, DB.Document doc)
    {
      var templatePath = GetTemplatePath("Generic Model");
      if (!File.Exists(templatePath))
      {
        throw new Exception($"Could not find Generic Model rft file - {templatePath}");
      }

      var famDoc = doc.Application.NewFamilyDocument(templatePath);
      
      using (var t = new DB.Transaction(famDoc, "Import DXF elements"))
      {
        t.Start();

        CreateDxfImport(objects, fileName, famDoc);

        t.Commit();
      }

      var famName = fileName;
      string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
      var so = new DB.SaveAsOptions();
      so.OverwriteExistingFile = true;
      
      famDoc.SaveAs(tempFamilyPath, so);
      famDoc.Close();
      
      Report.Log($"Created Dxf Import Family at: {tempFamilyPath}");

      Doc.LoadFamily(tempFamilyPath, new FamilyLoadOption(), out var fam);
      var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
      symbol.Activate();
      
      try
      {
        File.Delete(tempFamilyPath);
      }
      catch {}

      return Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);
    }

  }
}

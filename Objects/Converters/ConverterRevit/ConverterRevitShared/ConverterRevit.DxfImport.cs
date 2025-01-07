using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConverterRevitShared.Revit;
using Objects.Converters.DxfConverter;
using Objects.Geometry;
using Speckle.Core.Helpers;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.netDxf.Entities;
using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject DirectShapeToDxfImport(DirectShape ds, DB.Document doc = null)
  {
    var appObj = new ApplicationObject(ds.id, ds.speckle_type) { applicationId = ds.applicationId };
    var existing = CheckForExistingObject(ds);
    if (existing != null)
    {
      return existing;
    }

    var el = CreateDxfImport(new List<Base>(ds.baseGeometries), $"Speckle-Mesh-{ds.id}-{ds.applicationId}.dxf", 1, doc);
    appObj.Update(status: ApplicationObject.State.Created, createdId: el.UniqueId, convertedItem: el);
    return appObj;
  }

  public ApplicationObject DirectShapeToDxfImportFamily(DirectShape ds, DB.Document doc = null)
  {
    var appObj = new ApplicationObject(ds.id, ds.speckle_type) { applicationId = ds.applicationId };
    var existing = CheckForExistingObject(ds);
    if (existing != null)
    {
      return existing;
    }

    var el = CreateDxfImportFamily(
      new List<Base>(ds.baseGeometries),
      $"Speckle-Mesh-{ds.id}-{ds.applicationId}.dxf",
      1,
      doc
    );
    appObj.Update(status: ApplicationObject.State.Created, createdId: el.UniqueId, convertedItem: el);
    return appObj;
  }

  public ApplicationObject MeshToDxfImport(Mesh mesh, DB.Document doc = null)
  {
    var appObj = new ApplicationObject(mesh.id, mesh.speckle_type) { applicationId = mesh.applicationId };
    var existing = CheckForExistingObject(mesh);
    if (existing != null)
    {
      return existing;
    }

    var el = CreateDxfImport(new List<Base> { mesh }, $"Speckle-Mesh-{mesh.id}-{mesh.applicationId}.dxf", 1, doc);
    appObj.Update(status: ApplicationObject.State.Created, createdId: el.UniqueId, convertedItem: el);
    return appObj;
  }

  public ApplicationObject MeshToDxfImportFamily(Mesh mesh, DB.Document doc = null)
  {
    var appObj = new ApplicationObject(mesh.id, mesh.speckle_type) { applicationId = mesh.applicationId };
    var existing = CheckForExistingObject(mesh);
    if (existing != null)
    {
      return existing;
    }

    var el = CreateDxfImportFamily(new List<Base> { mesh }, $"Speckle-Mesh-{mesh.id}-{mesh.applicationId}", 1, doc);
    appObj.Update(status: ApplicationObject.State.Created, createdId: el.UniqueId, convertedItem: el);
    return appObj;
  }

  public ApplicationObject BrepToDxfImport(Brep brep, DB.Document doc)
  {
    var appObj = new ApplicationObject(brep.id, brep.speckle_type) { applicationId = brep.applicationId };
    var existing = CheckForExistingObject(brep);
    if (existing != null)
    {
      return existing;
    }

    var el = CreateDxfImport(new List<Base> { brep }, $"Speckle-Brep-{brep.id}-{brep.applicationId}", 1, doc);

    appObj.Update(status: ApplicationObject.State.Created, createdId: el.UniqueId, convertedItem: el);
    return appObj;
  }

  public ApplicationObject BrepToDxfImportFamily(Brep brep, DB.Document doc = null)
  {
    var appObj = new ApplicationObject(brep.id, brep.speckle_type) { applicationId = brep.applicationId };
    var existing = CheckForExistingObject(brep);
    if (existing != null)
    {
      return existing;
    }

    var el = CreateDxfImportFamily(new List<Base> { brep }, $"Speckle-Brep-{brep.id}-{brep.applicationId}", 1, doc);

    appObj.Update(status: ApplicationObject.State.Created, createdId: el.UniqueId, convertedItem: el);
    return appObj;
  }

  /// <summary>
  /// Creates a new Revit Element from a list of Base objects by exporting them to DXF and importing that file back.
  /// </summary>
  /// <param name="objects">The objects to be imported.</param>
  /// <param name="fileName">The filename for the temporary DXF file.</param>
  /// <param name="scaleFactor">The scale factor for the DXF.</param>
  /// <param name="doc">The document to import the DXF into.</param>
  /// <returns>The Element containing the imported DXF geometry.</returns>
  /// <exception cref="SpeckleException">Will throw an error if no objects where successfully converted.</exception>
  public DB.Element CreateDxfImport(List<Base> objects, string fileName, double scaleFactor = 1, DB.Document doc = null)
  {
    doc ??= Doc; // Use the global doc if no doc is passed in.
    var dxfConverter = new SpeckleDxfConverter { Settings = { PrettyMeshes = true, DocUnits = ModelUnits } };

    dxfConverter.SetContextDocument(null); // Resets the internal Doc.

    var collection = dxfConverter
      .ConvertToNative(objects)
      .SelectMany(o =>
      {
        if (o is IEnumerable<EntityObject> e)
        {
          return e;
        }

        return new List<EntityObject> { (EntityObject)o };
      })
      .Where(e => e != null)
      .ToList();

    if (collection.Count == 0)
    {
      throw new SpeckleException("No objects could be converted to DXF.");
    }

    dxfConverter.Doc.Entities.Add(collection.Where(x => x != null));

    var folderPath = Path.Combine(SpecklePathProvider.UserSpeckleFolderPath, "Temp", "Dxf");

    // Ensure directory exists
    if (!Directory.Exists(folderPath))
    {
      Directory.CreateDirectory(folderPath);
    }

    // Save the DXF file
    var path = Path.Combine(folderPath, fileName + ".dxf");
    dxfConverter.Doc.Save(path);

    // Create a 3D view to import the DXF file
    var typeId = doc.GetDefaultElementTypeId(DB.ElementTypeGroup.ViewType3D);
    var view = DB.View3D.CreatePerspective(doc, typeId);

    // Call Doc Import
    var success = doc.Import(
      path,
      new DB.DWGImportOptions
      {
        Unit = DB.ImportUnit.Meter,
        CustomScale = Units.GetConversionFactor("meter", ModelUnits) * scaleFactor
      },
      view,
      out var elementId
    );

    doc.Delete(view.Id);
    File.Delete(path);

    if (!success)
    {
      throw new SpeckleException($"Failed to import DXF file: {path}");
    }

    var el = doc.GetElement(elementId);
    el.Pinned = false;
    return el;
  }

  /// <summary>
  /// Creates a new Revit Family from a list of Base objects by exporting them to DXF; importing that DXF into a new Generic Model family, and returning
  /// </summary>
  /// <param name="objects">The objects to be imported.</param>
  /// <param name="fileName">The filename for the temporary DXF file.</param>
  /// <param name="scaleFactor">The scale factor for the DXF.</param>
  /// <param name="doc">The document to import the DXF into.</param>
  /// <returns>The Family instance containing the imported DXF geometry.</returns>
  /// <exception cref="SpeckleException">Will throw an error if no objects where successfully converted.</exception>
  public DB.FamilyInstance CreateDxfImportFamily(
    List<Base> objects,
    string fileName,
    double scaleFactor = 1,
    DB.Document doc = null
  )
  {
    doc ??= Doc; // Use the global doc if no doc is passed in.
    var templatePath = GetTemplatePath("Generic Model");
    if (!File.Exists(templatePath))
    {
      throw new Exception($"Could not find Generic Model rft file - {templatePath}");
    }

    var famDoc = doc.Application.NewFamilyDocument(templatePath);
    using (var t = new DB.Transaction(famDoc, "Import DXF elements"))
    {
      t.Start();
      try
      {
        CreateDxfImport(objects, fileName, scaleFactor, famDoc);
      }
      catch (SpeckleException e)
      {
        t.RollBack();
        famDoc.Close();
        throw;
      }
      t.Commit();
    }

    var tempFamilyPath = Path.Combine(Path.GetTempPath(), fileName + ".rfa");
    var so = new DB.SaveAsOptions { OverwriteExistingFile = true };

    famDoc.SaveAs(tempFamilyPath, so);
    famDoc.Close();

    Report.Log($"Created Dxf Import Family at: {tempFamilyPath}");

    Doc.LoadFamily(tempFamilyPath, new FamilyLoadOption(), out var fam);
    var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
    symbol.Activate();

    if (File.Exists(tempFamilyPath))
    {
      File.Delete(tempFamilyPath);
    }

    return Doc.Create.NewFamilyInstance(DB.XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);
  }
}

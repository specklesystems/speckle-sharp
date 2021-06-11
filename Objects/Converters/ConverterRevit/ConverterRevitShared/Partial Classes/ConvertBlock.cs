using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Mesh = Objects.Geometry.Mesh;
using Parameter = Objects.BuiltElements.Revit.Parameter;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // Creates a generic model instance in a project or family doc
    public string BlockInstanceToNative(BlockInstance instance, Document familyDoc = null)
    {

      string result = null;

      // Base point
      var basePoint = PointToNative(instance.insertionPoint);

      // Get or make family from block definition
      FamilySymbol familySymbol = new FilteredElementCollector(Doc)
        .OfClass(typeof(Family))
        .OfType<Family>()
        .FirstOrDefault(f => f.Name.Equals("SpeckleBlock_" + instance.blockDefinition.name))
        ?.GetFamilySymbolIds()
        .Select(id => Doc.GetElement(id))
        .OfType<FamilySymbol>()
        .First();

      if (familySymbol == null)
      {
        var familyPath = BlockDefinitionToNative(instance.blockDefinition);

        if (familyDoc != null)
        {
          if (familyDoc.LoadFamily(familyPath, new FamilyLoadOption(), out var fam));
            familySymbol = familyDoc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
        }
        else
        {
          if (Doc.LoadFamily(familyPath, new FamilyLoadOption(), out var fam))
            familySymbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as DB.FamilySymbol;
        }

        familySymbol.Activate();
        //File.Delete(familyPath);
      }

      // see if this is a nested family instance or to be inserted in project
      FamilyInstance _instance = null;
      if (familyDoc != null)
      {
        _instance = familyDoc.FamilyCreate.NewFamilyInstance(basePoint, familySymbol, DB.Structure.StructuralType.NonStructural);
        familyDoc.Regenerate();
      }
      else
      {
        _instance = Doc.Create.NewFamilyInstance(basePoint, familySymbol, DB.Structure.StructuralType.NonStructural);
        Doc.Regenerate();
      }

      // transform
      if (_instance != null)
      {
        if (MatrixDecompose(instance.transform, out double rotation))
        {
          try
          {
            // some point based families don't have a rotation, so keep this in a try catch
            if (rotation != (_instance.Location as LocationPoint).Rotation)
            {
              var axis = DB.Line.CreateBound(new XYZ(basePoint.X, basePoint.Y, 0), new XYZ(basePoint.X, basePoint.Y, 1000));
              (_instance.Location as LocationPoint).Rotate(axis, rotation - (_instance.Location as LocationPoint).Rotation);
            }
          }
          catch { }

        }
        SetInstanceParameters(_instance, instance);
        result = "success";
      }

      return result;
    }

    // TODO: fix unit conversions since block geometry is being converted inside a new family document, which potentially has different unit settings from the main doc.
    // This could be done by passing in an option Document argument for all conversions that defaults to the main doc (annoying)
    // I suspect this also needs to be fixed for freeform elements
    private string BlockDefinitionToNative(BlockDefinition definition)
    {
      // create a family to represent a block definition
      // TODO: rename block with stream commit info prefix taken from UI - need to figure out cleanest way of storing this in the doc for retrieval by converter
      var templatePath = GetTemplatePath("Generic Model");
      if (!File.Exists(templatePath))
      {
        throw new Exception($"Could not find template file - {templatePath}");
      }

      var famDoc = Doc.Application.NewFamilyDocument(templatePath);

      // convert definition geometry to native
      var solids = new List<DB.Solid>();
      var curves = new List<DB.Curve>();
      var blocks = new List<BlockInstance>();
      foreach (var geometry in definition.geometry)
      {
        switch (geometry)
        {
          case Brep brep:
            try
            {
              var solid = BrepToNative(geometry as Brep);
              solids.Add(solid);
            }
            catch (Exception e)
            {
              ConversionErrors.Add(new SpeckleException($"Could not convert block {definition.id} brep to native, falling back to mesh representation.", e));
              var brepMeshSolids = MeshToNative(brep.displayMesh, DB.TessellatedShapeBuilderTarget.Solid, DB.TessellatedShapeBuilderFallback.Abort)
                  .Select(m => m as DB.Solid);
              solids.AddRange(brepMeshSolids);
            }
            break;
          case Mesh mesh:
            var meshSolids = MeshToNative(mesh, DB.TessellatedShapeBuilderTarget.Solid, DB.TessellatedShapeBuilderFallback.Abort)
                .Select(m => m as DB.Solid);
            solids.AddRange(meshSolids);
            break;
          case ICurve curve:
            try
            {
              var modelCurves = CurveToNative(geometry as ICurve);
              foreach (DB.Curve modelCurve in modelCurves)
                curves.Add(modelCurve);
            }
            catch (Exception e)
            {
              ConversionErrors.Add(new SpeckleException($"Could not convert block {definition.id} curve to native.", e));
            }
            break;
          case BlockInstance instance:
            blocks.Add(instance);
            break;
        }
      }

      using (DB.Transaction t = new DB.Transaction(famDoc, "Create Block Geometry Elements"))
      {
        t.Start();

        solids.ForEach(o => { DB.FreeFormElement.Create(famDoc, o); });
        curves.ForEach(o => { famDoc.FamilyCreate.NewModelCurve(o, NewSketchPlaneFromCurve(o, famDoc)); });
        blocks.ForEach(o => { BlockInstanceToNative(o, famDoc); });

        t.Commit();
      }

      var famName = "SpeckleBlock_" + definition.name;
      string familyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
      var so = new DB.SaveAsOptions();
      so.OverwriteExistingFile = true;
      famDoc.SaveAs(familyPath, so);
      famDoc.Close();

      return familyPath;
    }

    private bool MatrixDecompose(double[] m, out double rotation)
    {
      var matrix = new Matrix4x4(
        (float)m[0], (float)m[1], (float)m[2], (float)m[3],
        (float)m[4], (float)m[5], (float)m[6], (float)m[7],
        (float)m[8], (float)m[9], (float)m[10], (float)m[11],
        (float)m[12], (float)m[13], (float)m[14], (float)m[15]);

      if (Matrix4x4.Decompose(matrix, out Vector3 _scale, out Quaternion _rotation, out Vector3 _translation))
      {
        rotation = Math.Acos(_rotation.W) * 2;
        return true;
      }
      else
      {
        rotation = 0;
        return false;
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Core.Logging;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // Creates a generic model instance in a project or family doc
    public Group BlockInstanceToNative(BlockInstance instance)
    {

      string result = null;

      // Base point
      var basePoint = PointToNative(instance.GetInsertionPoint());

      // Get or make family from block definition
      GroupType block_def = new FilteredElementCollector(Doc)
        .OfClass(typeof(GroupType))
        .OfType<GroupType>()
        .FirstOrDefault(f => f.Name.Equals("SpeckleBlock_" + instance.blockDefinition.name)) ??
        BlockDefinitionToNative(instance.blockDefinition);

      Group _instance = Doc.Create.PlaceGroup(basePoint, block_def);

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

      }
      Report.Log($"Created Block {_instance.Id}");
      return _instance;
    }

    private GroupType BlockDefinitionToNative(BlockDefinition definition)
    {
      // create a family to represent a block definition
      // TODO: rename block with stream commit info prefix taken from UI - need to figure out cleanest way of storing this in the doc for retrieval by converter

      // convert definition geometry to native
      var ids = new List<ElementId>();
      foreach (var geometry in definition.geometry)
      {
        switch (geometry)
        {
          case Brep brep:
            var brepShape = DirectShapeToNative(brep).NativeObject as DB.DirectShape;
            ids.Add(brepShape?.Id);
            break;
          case Mesh mesh:
            var meshShape = DirectShapeToNative(mesh).NativeObject as DB.DirectShape;
            ids.Add(meshShape.Id);
            break;
          case ICurve curve:
            try
            {
              var modelCurves = CurveToNative(curve).Cast<DB.Curve>().ToList();
              modelCurves.ForEach(o =>
              {
                var modelCurve = Doc.Create.NewModelCurve(o, NewSketchPlaneFromCurve(o, Doc));
                ids.Add(modelCurve.Id);
              });
            }
            catch (Exception e)
            {
              Report.LogConversionError(new SpeckleException($"Could not convert block {definition.id} curve to native.", e));
            }
            break;
          case BlockInstance instance:
            var grp = BlockInstanceToNative(instance);
            ids.Add(grp.Id);
            break;
        }
      }

      var group = Doc.Create.NewGroup(ids);
      var groupType = group.GroupType;
      Doc.Delete(group.Id);
      groupType.Name = "SpeckleBlock_" + definition.name;
      return groupType;
    }

    private bool MatrixDecompose(double[ ] m, out double rotation)
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

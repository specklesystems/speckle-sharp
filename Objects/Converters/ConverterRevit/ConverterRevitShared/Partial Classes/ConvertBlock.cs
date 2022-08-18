using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BlockInstance = Objects.Other.BlockInstance;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;
using Transform = Objects.Other.Transform;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public Group BlockInstanceToNative(BlockInstance instance, Transform transform = null)
    {
      // need to combine the two transforms, but i'm stupid and did it wrong so leaving like this for now
      if (transform != null)
        transform *= instance.transform;
      else
        transform = instance.transform;

      // convert definition geometry to native
      var breps = new List<Brep>();
      var meshes = new List<Mesh>();
      var curves = new List<DB.Curve>();
      var blocks = new List<BlockInstance>();
      foreach (var geometry in instance.blockDefinition.geometry)
      {
        switch (geometry)
        {
          case Brep brep:
            var success = brep.TransformTo(transform, out Brep tbrep);
            if (success)
              breps.Add(tbrep);
            else
            {
              Report.LogConversionError(new SpeckleException(
                $"Could not convert block {instance.id} brep to native, falling back to mesh representation."));
              meshes.AddRange(tbrep.displayValue);
            }
            break;
          case Mesh mesh:
            mesh.TransformTo(transform, out Mesh tmesh);
            meshes.Add(tmesh);
            break;
          case ICurve curve:
            try
            {
              if (curve is ITransformable tCurve)
              {
                tCurve.TransformTo(transform, out tCurve);
                curve = (ICurve)tCurve;
              }

              var modelCurves = CurveToNative(curve);
              curves.AddRange(modelCurves.Cast<DB.Curve>());
            }
            catch (Exception e)
            {
              Report.LogConversionError(
                new SpeckleException($"Could not convert block {instance.id} curve to native.", e));
            }

            break;
          case BlockInstance blk:
            blocks.Add(blk);
            break;
        }
      }

      var ids = new List<ElementId>();
      breps.ForEach(o =>
      {
        var ds = DirectShapeToNative(o).NativeObject as DB.DirectShape;
        if (ds != null)
          ids.Add(ds.Id);
      });
      meshes.ForEach(o =>
      {
        var ds = DirectShapeToNative(o).NativeObject as DB.DirectShape;
        if (ds != null)
          ids.Add(ds.Id);
        ids.Add(ds.Id);
      });
      curves.ForEach(o =>
      {
        var mc = Doc.Create.NewModelCurve(o, NewSketchPlaneFromCurve(o, Doc));
        if (mc != null)
          ids.Add(mc.Id);
      });
      blocks.ForEach(o =>
      {
        var block = BlockInstanceToNative(o, transform);
        if (block != null)
          ids.Add(block.Id);
      });

      if (!ids.Any())
        return null;

      var group = Doc.Create.NewGroup(ids);
      group.GroupType.Name = $"SpeckleBlock_{instance.blockDefinition.name}_{instance.applicationId ?? instance.id}";
      Report.Log($"Created Group '{ group.GroupType.Name}' {group.Id}");
      return group;
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
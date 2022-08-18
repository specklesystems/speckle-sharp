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
      // get app object
      var appObj = Report.GetReportObject(instance.id, out int index) ? Report.ReportObjects[index] : null;

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
              appObj.Update(logItem: $"Could not convert block brep to native, using mesh fallback value instead");
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
              appObj.Update(logItem: $"Could not convert block curve to native: {e.Message}");
            }
            break;
          case BlockInstance blk:
            blocks.Add(blk);
            break;
        }
      }

      var ids = new List<ElementId>();
      int brepCount = 0;
      breps.ForEach(o =>
      {
        var ds = DirectShapeToNative(o).Converted.FirstOrDefault() as DB.DirectShape;
        if (ds != null)
        {
          ids.Add(ds.Id);
          brepCount++;
        }
      });
      int skippedBreps = breps.Count - brepCount;

      int meshCount = 0;
      meshes.ForEach(o =>
      {
        var ds = DirectShapeToNative(o).Converted.FirstOrDefault() as DB.DirectShape;
        if (ds != null)
        {
          ids.Add(ds.Id);
          meshCount++;
        } 
      });
      int skippedMeshes = meshes.Count - meshCount;

      int curveCount = 0;
      curves.ForEach(o =>
      {
        var mc = Doc.Create.NewModelCurve(o, NewSketchPlaneFromCurve(o, Doc));
        if (mc != null)
        {
          ids.Add(mc.Id);
          curveCount++;
        }
      });
      int skippedCurves = curves.Count - curveCount;

      int blockCount = 0;
      blocks.ForEach(o =>
      {
        var block = BlockInstanceToNative(o, transform);
        if (block != null)
        {
          ids.Add(block.Id);
          blockCount++;
        }
      });
      int skippedBlocks = blocks.Count - blockCount;

      if (!ids.Any())
      {
        appObj.Update(status: Speckle.Core.Models.ApplicationObject.State.Failed, logItem: $"No geometry could be created");
        return null;
      }

      Group group = null;
      try
      {
        group = Doc.Create.NewGroup(ids);
        group.GroupType.Name = $"SpeckleBlock_{instance.blockDefinition.name}_{instance.applicationId ?? instance.id}";
        string skipped = $"{(skippedBreps > 0 ? $"{skippedBreps} breps " : "")}{(skippedMeshes > 0 ? $"{skippedMeshes} meshes " : "")}{(skippedCurves > 0 ? $"{skippedCurves} curves " : "")}{(skippedBlocks > 0 ? $"{skippedBlocks} blocks " : "")}";
        if (!string.IsNullOrEmpty(skipped)) appObj.Update(logItem: $"Skipped {skipped}");
        appObj.Update(status: Speckle.Core.Models.ApplicationObject.State.Created, createdId: group.UniqueId, convertedItem: group, logItem: $"Assigned name: {group.GroupType.Name}");
      }
      catch
      {
        appObj.Update(status: Speckle.Core.Models.ApplicationObject.State.Failed, logItem: $"Group could not be created");
      }
      
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
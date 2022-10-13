﻿using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
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
    public ApplicationObject BlockInstanceToNative(BlockInstance instance, Transform transform = null)
    {
      var docObj = GetExistingElementByApplicationId(instance.applicationId);
      var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      var isUpdate = false;
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Update)
      {
        try
        {
          Doc.Delete(docObj.Id);
          isUpdate = true;
        }
        catch
        {
          //something went wrong, re-create it
        }
      }

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
      int skippedBreps = breps.Count;
      breps.ForEach(o =>
      {
        var ds = DirectShapeToNative(o).Converted.FirstOrDefault() as DB.DirectShape;
        if (ds != null)
        {
          ids.Add(ds.Id);
          skippedBreps--;
        }
      });

      int skippedMeshes = meshes.Count;
      meshes.ForEach(o =>
      {
        var ds = DirectShapeToNative(o).Converted.FirstOrDefault() as DB.DirectShape;
        if (ds != null)
        {
          ids.Add(ds.Id);
          skippedMeshes--;
        } 
      });

      int skippedCurves = curves.Count;
      curves.ForEach(o =>
      {
        var mc = Doc.Create.NewModelCurve(o, NewSketchPlaneFromCurve(o, Doc));
        if (mc != null)
        {
          ids.Add(mc.Id);
          skippedCurves--;
        }
      });

      int skippedBlocks = blocks.Count;
      blocks.ForEach(o =>
      {
        var block = BlockInstanceToNative(o, transform);
        if (block != null)
        {
          var nestedBlock = block.Converted.FirstOrDefault() as Group;
          ids.Add(nestedBlock.Id);
          skippedBlocks--;
        }
      });

      if (!ids.Any())
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"No geometry could be created");
        Report.Log(appObj);
        return null;
      }

      Group group = null;
      try
      {
        group = Doc.Create.NewGroup(ids);
        group.GroupType.Name = $"SpeckleBlock_{RemoveProhibitedCharacters(instance.blockDefinition.name)}_{instance.applicationId ?? instance.id}";
        string skipped = $"{(skippedBreps > 0 ? $"{skippedBreps} breps " : "")}{(skippedMeshes > 0 ? $"{skippedMeshes} meshes " : "")}{(skippedCurves > 0 ? $"{skippedCurves} curves " : "")}{(skippedBlocks > 0 ? $"{skippedBlocks} blocks " : "")}";
        if (!string.IsNullOrEmpty(skipped)) appObj.Update(logItem: $"Skipped {skipped}");
        var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
        appObj.Update(status: state, createdId: group.UniqueId, convertedItem: group, logItem: $"Assigned name: {group.GroupType.Name}");
      }
      catch
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Group could not be created");
      }
      return appObj;
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
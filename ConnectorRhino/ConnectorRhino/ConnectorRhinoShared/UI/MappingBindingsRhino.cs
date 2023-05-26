using System;
using System.Collections.Generic;
using System.Linq;
using DesktopUI2;
using DesktopUI2.ViewModels.MappingTool;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;

namespace SpeckleRhino;

public class MappingBindingsRhino : MappingsBindings
{
  private static string SpeckleMappingKey = "SpeckleMapping";
  private static string SpeckleMappingViewKey = "SpeckleMappingView";

  private MappingsDisplayConduit Display;

  public MappingBindingsRhino()
  {
    Display = new MappingsDisplayConduit();
    Display.Enabled = true;
  }

  public override MappingSelectionInfo GetSelectionInfo()
  {
    try
    {
      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
      var result = new List<Schema>();

      foreach (var obj in selection)
      {
        var schemas = GetObjectSchemas(obj);

        if (!result.Any())
          result = schemas;
        else
          //intersect lists
          //TODO: if some elements already have a schema and values are different
          //we should default to an empty schema, instead of potentially restoring the one with values
          result = result.Where(x => schemas.Any(y => y.Name == x.Name)).ToList();

        //incompatible selection
        if (!result.Any())
          return new MappingSelectionInfo(new List<Schema>(), selection.Count);
      }

      return new MappingSelectionInfo(result, selection.Count);
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Could not get selection info: {exceptionMessage}", ex.Message);
      return new MappingSelectionInfo(new List<Schema>(), 0);
    }
  }

  /// <summary>
  /// For a give Rhino Object find all applicable schemas and retrive any existing one already applied
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  private List<Schema> GetObjectSchemas(RhinoObject obj)
  {
    var result = new List<Schema>();

    try
    {
      var existingSchema = GetExistingObjectSchema(obj);
      if (existingSchema != null)
        result.Add(existingSchema);

      if (obj is InstanceObject)
        result.Add(new RevitFamilyInstanceViewModel());
      else
        switch (obj.Geometry)
        {
          case Mesh m:
            if (!result.Any(x => typeof(DirectShapeFreeformViewModel) == x.GetType()))
              result.Add(new DirectShapeFreeformViewModel());
            if (!m.IsClosed)
              result.Add(new RevitTopographyViewModel());
            break;

          case Brep b:
            if (!result.Any(x => typeof(DirectShapeFreeformViewModel) == x.GetType()))
              result.Add(new DirectShapeFreeformViewModel());

            var brepSurfaceSchemas = EvaluateBrepSurfaceSchemas(b);
            result.AddRange(brepSurfaceSchemas);
            break;

          case Extrusion e:
            result.Add(new DirectShapeFreeformViewModel());

            if (e.ProfileCount > 1)
              break;
            var extrusionBrp = e.ToBrep(false);
            var extrusionSurfaceSchemas = EvaluateBrepSurfaceSchemas(extrusionBrp, true);
            result.AddRange(extrusionSurfaceSchemas);
            break;

          case Curve c:
            if (c.IsLinear())
            {
              result.Add(new RevitBeamViewModel());
              result.Add(new RevitBraceViewModel());
              result.Add(new RevitColumnViewModel());
              result.Add(new RevitPipeViewModel());
              result.Add(new RevitDuctViewModel());

              result.Add(new RevitDefaultBeamViewModel());
              result.Add(new RevitDefaultBraceViewModel());
              result.Add(new RevitDefaultColumnViewModel());
              result.Add(new RevitDefaultPipeViewModel());
              result.Add(new RevitDefaultDuctViewModel());
            }
            else if (c.IsPlanar())
            {
              // If the curve is non-linear, but is planar, it can still be a beam.
              result.Add(new RevitDefaultBeamViewModel());
              result.Add(new RevitBeamViewModel());
            }

            //if (c.IsLinear() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
            //if (c.IsLinear() && c.PointAtEnd.X == c.PointAtStart.X && c.PointAtEnd.Y == c.PointAtStart.Y) cats.Add(Column);
            //if (c.IsArc() && !c.IsCircle() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
            break;

          case Point p:
            result.Add(new RevitFamilyInstanceViewModel());
            break;
        }
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Error(ex, "Could not get object schemas: {exceptionMessage}", ex.Message);
    }

    return result;
  }

  /// <summary>
  /// Evaluates a brep to test for single surface, planarity, edges, and normal direction.
  /// </summary>
  /// <param name="b"></param>
  public List<Schema> EvaluateBrepSurfaceSchemas(Brep b, bool isExtrusion = false)
  {
    var schemas = new List<Schema>();
    if (b.Surfaces.Count != 1)
      return schemas;

    bool IsPlanar(Surface srf, out bool isHorizontal, out bool isVertical)
    {
      isHorizontal = false;
      isVertical = false;

      if (srf.TryGetPlane(out Plane p))
      {
        Vector3d normal = p.Normal;
        if (Math.Abs(Math.Abs(normal.Z) - 1) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
        {
          isHorizontal = true;
        }
        else if (Math.Abs(normal.Z) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
        {
          isVertical = true;
        }
        return true;
      }

      return false;
    }

    bool HasPlanarBottomEdge(Brep brp)
    {
      var brpCurves = b.DuplicateNakedEdgeCurves(true, false); // see if the bottom curve is parallel to worldxy
      var lowestZ = brp.GetBoundingBox(false).Min.Z;
      var bottomCrv = brpCurves.Where(
        o =>
          new Vector3d(
            o.PointAtEnd.X - o.PointAtStart.X,
            o.PointAtEnd.Y - o.PointAtStart.Y,
            o.PointAtEnd.Z - o.PointAtStart.Z
          ).IsPerpendicularTo(Vector3d.ZAxis, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
          && o.PointAtStart.Z - lowestZ <= RhinoDoc.ActiveDoc.ModelAbsoluteTolerance
      );
      if (bottomCrv.Any() && (bottomCrv.First().IsLinear() || bottomCrv.First().IsArc()))
      {
        return true;
      }
      return false;
    }

    var surface = b.Surfaces.First();
    if (IsPlanar(surface, out bool isHorizontal, out bool isVertical)) // if this is a planar surface, determine if it can be a wall or floor
    {
      if (isHorizontal)
      {
        schemas.Add(new RevitFloorViewModel());
        schemas.Add(new RevitDefaultFloorViewModel());
      }
      else if (isVertical)
      {
        if (isExtrusion)
        {
          schemas.Add(new RevitWallViewModel());
          schemas.Add(new RevitProfileWallViewModel());
        }
        else
        {
          if (HasPlanarBottomEdge(b))
          {
            schemas.Add(new RevitWallViewModel());
          }
          schemas.Add(new RevitProfileWallViewModel());
        }
        schemas.Add(new RevitFaceWallViewModel());
        schemas.Add(new RevitDefaultWallViewModel());
      }
      else
      {
        schemas.Add(new RevitFaceWallViewModel());
      }
    }
    else
    {
      schemas.Add(new RevitFaceWallViewModel());
    }

    return schemas;
  }

  private Schema GetExistingObjectSchema(RhinoObject obj)
  {
    var viewModel = obj.Attributes.GetUserString(SpeckleMappingViewKey);

    if (string.IsNullOrEmpty(viewModel))
      return null;

    try
    {
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

      return JsonConvert.DeserializeObject<Schema>(viewModel, settings);
    }
    catch
    {
      return null;
    }
  }

  public override void SetMappings(string schema, string viewModel)
  {
    var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
    foreach (var obj in selection)
    {
      obj.Attributes.SetUserString(SpeckleMappingKey, schema);
      obj.Attributes.SetUserString(SpeckleMappingViewKey, viewModel);
    }

    SpeckleRhinoConnectorPlugin.Instance.ExistingSchemaLogExpired = true;
  }

  public override void ClearMappings(List<string> ids)
  {
    foreach (var id in ids)
      try
      {
        var obj = RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id));
        if (obj == null)
          continue;
        obj.Attributes.DeleteUserString(SpeckleMappingKey);
        obj.Attributes.DeleteUserString(SpeckleMappingViewKey);
      }
      catch { }

    SpeckleRhinoConnectorPlugin.Instance.ExistingSchemaLogExpired = true;
  }

  public override List<Schema> GetExistingSchemaElements()
  {
    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

    var objects = RhinoDoc.ActiveDoc.Objects.FindByUserString(SpeckleMappingViewKey, "*", true);
    var schemas = objects
      .Select(
        obj => JsonConvert.DeserializeObject<Schema>(obj.Attributes.GetUserString(SpeckleMappingViewKey), settings)
      )
      .ToList();

    //add the object id to the schema so we can easily highlight/clear them
    for (var i = 0; i < schemas.Count; i++)
      schemas[i].ApplicationId = objects[i].Id.ToString();

    return schemas;
  }

  public override void HighlightElements(List<string> ids)
  {
    try
    {
      Display.ObjectIds = ids;
      RhinoDoc.ActiveDoc?.Views.Redraw();
    }
    catch (Exception ex)
    {
      //fail silently
    }
  }

  public override void SelectElements(List<string> ids)
  {
    try
    {
      RhinoDoc.ActiveDoc.Objects.UnselectAll();
      RhinoDoc.ActiveDoc.Objects.Select(ids.Select(x => Guid.Parse(x)));
      RhinoDoc.ActiveDoc?.Views.Redraw();
    }
    catch (Exception ex)
    {
      //fail silently
    }
  }
}

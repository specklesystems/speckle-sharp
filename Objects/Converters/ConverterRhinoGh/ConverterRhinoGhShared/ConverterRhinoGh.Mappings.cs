#if GRASSHOPPER
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.Other;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using RH = Rhino.Geometry;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  private Base MappingToSpeckle(string mapping, RhinoObject @object, List<string> notes)
  {
    var defaultPreprocess = PreprocessGeometry;
    PreprocessGeometry = true;
    Base schemaObject = Operations.Deserialize(mapping);
    try
    {
      switch (schemaObject)
      {
        case RevitProfileWall o:
          var profileWallBrep = @object.Geometry is RH.Brep profileB
            ? profileB
            : ((RH.Extrusion)@object.Geometry)?.ToBrep();
          if (profileWallBrep == null)
          {
            throw new ArgumentException("Wall geometry can only be a brep or extrusion");
          }

          var edges = profileWallBrep.DuplicateNakedEdgeCurves(true, false);
          var profileCurve = RH.Curve.JoinCurves(edges);
          if (profileCurve.Count() != 1)
          {
            throw new Exception("Surface external edges should be joined into 1 curve");
          }

          var speckleProfileCurve = CurveToSpeckle(profileCurve.First());
          var profile = new Polycurve()
          {
            segments = new List<ICurve>() { speckleProfileCurve },
            length = profileCurve.First().GetLength(),
            closed = profileCurve.First().IsClosed,
            units = ModelUnits
          };
          o.profile = profile;
          break;

        case RevitFaceWall o:
          var faceWallBrep = @object.Geometry is RH.Brep faceB ? faceB : ((RH.Extrusion)@object.Geometry)?.ToBrep();
          o.brep = BrepToSpeckle(faceWallBrep);
          break;

        //NOTE: this works for BOTH the Wall.cs class and RevitWall.cs class etc :)
        case Wall o:
          var wallBrep = @object.Geometry is RH.Brep wallB ? wallB : ((RH.Extrusion)@object.Geometry)?.ToBrep();
          if (wallBrep == null)
          {
            throw new ArgumentException("Wall geometry can only be a brep or extrusion");
          }

          var wallEdges = wallBrep.DuplicateNakedEdgeCurves(true, false);
          var topBottomEdges = wallEdges
            .Where(o => Math.Abs(o.PointAtStart.Z - o.PointAtEnd.Z) < Doc.ModelAbsoluteTolerance)
            .OrderBy(o => o.PointAtStart.Z)
            .ToList();
          if (topBottomEdges.Count != 2)
          {
            throw new ArgumentException("Wall geometry has invalid edges");
          }

          var bottomCrv = topBottomEdges.First();
          var topCrv = topBottomEdges.Last();
          var height = topCrv.PointAtStart.Z - bottomCrv.PointAtStart.Z;
          o.height = height;
          o.baseLine = CurveToSpeckle(bottomCrv);
          break;

        case Floor o:
          var floorBrep = (RH.Brep)@object.Geometry;
          var extFloorCurves = GetSurfaceBrepEdges(floorBrep); // extract outline
          var intFloorCurves = GetSurfaceBrepEdges(floorBrep, getInterior: true); // extract voids
          o.outline = extFloorCurves.First();
          o.voids = intFloorCurves;
          break;

        case Ceiling o:
          var ceilingBrep = (RH.Brep)@object.Geometry;
          var extCeilingCurves = GetSurfaceBrepEdges(ceilingBrep); // extract outline
          var intCeilingCurves = GetSurfaceBrepEdges(ceilingBrep, getInterior: true); // extract voids
          o.outline = extCeilingCurves.First();
          o.voids = intCeilingCurves;
          break;

        case Roof o:
          var brep = (RH.Brep)@object.Geometry;
          var extRoofCurves = GetSurfaceBrepEdges(brep); // extract outline
          var intRoofCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract voids
          o.outline = extRoofCurves.First();
          o.voids = intRoofCurves;
          break;

        case Beam o:
          o.baseLine = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Brace o:
          o.baseLine = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Column o:
          o.baseLine = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Pipe o:
          o.baseCurve = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Duct o:
          o.baseCurve = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case RevitTopography o:
          o.baseGeometry = MeshToSpeckle((RH.Mesh)@object.Geometry);
          break;

        case DirectShape o:
          if (string.IsNullOrEmpty(o.name))
          {
            o.name = "Speckle Mapper Shape";
          }

          if (@object.Geometry as RH.Brep != null)
          {
            o.baseGeometries = new List<Base> { BrepToSpeckle((RH.Brep)@object.Geometry) };
          }
          else if (@object.Geometry as RH.Mesh != null)
          {
            o.baseGeometries = new List<Base> { MeshToSpeckle((RH.Mesh)@object.Geometry) };
          }

          break;

        case FreeformElement o:
          if (@object.Geometry as RH.Brep != null)
          {
            o.baseGeometries = new List<Base> { BrepToSpeckle((RH.Brep)@object.Geometry) };
          }
          else if (@object.Geometry as RH.Mesh != null)
          {
            o.baseGeometries = new List<Base> { MeshToSpeckle((RH.Mesh)@object.Geometry) };
          }

          break;

        case FamilyInstance o:
          if (@object.Geometry is RH.Point p)
          {
            o.basePoint = PointToSpeckle(p);
          }
          else if (@object is InstanceObject instanceObject)
          {
            var block = BlockInstanceToSpeckle(instanceObject);
            var plane = block.GetInsertionPlane();
            o.basePoint = plane.origin;
            var angle = RH.Vector3d.VectorAngle(RH.Vector3d.XAxis, VectorToNative(plane.xdir), RH.Plane.WorldXY);
            o.rotation = angle;
          }
          break;
        case MappedBlockWrapper o:
          if (@object is InstanceObject instance)
          {
            o.instance = BlockInstanceToSpeckle(instance);
          }

          break;
      }

      schemaObject.applicationId = @object.Id.ToString();
      schemaObject["units"] = ModelUnits;

      notes.Add($"Attached {schemaObject.speckle_type} schema");
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      notes.Add($"Could not attach {schemaObject.speckle_type} schema: {ex.Message}");
    }

    PreprocessGeometry = defaultPreprocess;
    return schemaObject;
  }

  // edge curve convenience method
  private List<ICurve> GetSurfaceBrepEdges(
    RH.Brep brep,
    bool getExterior = true,
    bool getInterior = false,
    bool getBottom = false
  )
  {
    double tol = Doc.ModelAbsoluteTolerance * 1;

    RH.Curve[] brpCurves = null;
    if (getInterior)
    {
      brpCurves = brep.DuplicateNakedEdgeCurves(false, true);
    }
    else
    {
      brpCurves = brep.DuplicateNakedEdgeCurves(true, false);
    }

    if (getBottom)
    {
      var bottomCrv = brpCurves
        .Where(o => o.IsLinear())
        ?.Where(o =>
          new RH.Vector3d(
            o.PointAtEnd.X - o.PointAtStart.X,
            o.PointAtEnd.Y - o.PointAtStart.Y,
            o.PointAtEnd.Z - o.PointAtStart.Z
          ).IsPerpendicularTo(RH.Vector3d.ZAxis)
        )
        ?.Aggregate((curMin, o) => curMin == null || o.PointAtStart.Z < curMin.PointAtStart.Z ? o : curMin);
      if (bottomCrv != null)
      {
        brpCurves = new[] { bottomCrv };
      }
    }

    List<ICurve> outCurves = null;
    if (brpCurves != null && brpCurves.Count() > 0)
    {
      outCurves =
        brpCurves.Count() == 1
          ? new List<ICurve> { (ICurve)ConvertToSpeckle(brpCurves[0]) }
          : RH.Curve.JoinCurves(brpCurves, tol).Select(o => (ICurve)ConvertToSpeckle(o)).ToList();
    }

    return outCurves;
  }
}

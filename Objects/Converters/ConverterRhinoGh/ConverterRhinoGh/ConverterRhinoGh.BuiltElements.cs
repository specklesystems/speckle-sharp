using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhino;
using Wall = Objects.BuiltElements.Wall;
using Floor = Objects.BuiltElements.Floor;
using Ceiling = Objects.BuiltElements.Ceiling;
using Roof = Objects.BuiltElements.Roof;
using RH = Rhino.Geometry;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public Wall BrepToSpeckleWall(RH.Brep brep)
    {
      Wall wall = null;
      BoundingBox brepBox = brep.GetBoundingBox(false);
      double height = brepBox.Max.Z - brepBox.Min.Z; // extract height
      var bottomCurves = GetSurfaceBrepEdges(brep, getBottom: true); // extract baseline
      if (bottomCurves != null && height > 0)
        wall = new Wall(height, bottomCurves[0]);
      return wall;
    }

    public Floor BrepToSpeckleFloor(RH.Brep brep)
    {
      Floor floor = null;
      var extCurves = GetSurfaceBrepEdges(brep, getExterior: true); // extract outline
      var intCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract voids
      if (extCurves != null)
        floor = new Floor(extCurves[0], intCurves);
      return floor;
    }

    public Ceiling BrepToSpeckleCeiling(RH.Brep brep)
    {
      Ceiling ceiling = null;
      var extCurves = GetSurfaceBrepEdges(brep, getExterior: true); // extract outline
      var intCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract voids
      if (extCurves != null)
        ceiling = new Ceiling(extCurves[0], intCurves);
      return ceiling;
    }

    public Roof BrepToSpeckleRoof(RH.Brep brep)
    {
      Roof roof = null;
      var extCurves = GetSurfaceBrepEdges(brep, getExterior: true); // extract outline
      var intCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract voids
      if (extCurves != null)
        roof = new Roof(extCurves[0], intCurves);
      return roof;
    }

    // edge curve convenience method
    private List<ICurve> GetSurfaceBrepEdges(RH.Brep brep, bool getExterior = true, bool getInterior = false, bool getBottom = false)
    {
      double tol = Doc.ModelAbsoluteTolerance * 1;

      RH.Curve[] brpCurves = null;
      if (getInterior)
        brpCurves = brep.DuplicateNakedEdgeCurves(false, true);
      else
        brpCurves = brep.DuplicateNakedEdgeCurves(true, false);
      if (getBottom)
      {
        double lowestPt = brpCurves.Min(o => o.PointAtStart.Z);
        brpCurves = brpCurves.Where(o => o.PointAt(0.5).Z == lowestPt).ToArray();
      }

      List<ICurve> outCurves = null;
      if (brpCurves != null)
        outCurves = RH.Curve.JoinCurves(brpCurves, tol).Select(o => (ICurve)ConvertToSpeckle(o)).ToList();
      return outCurves;
    }
  }
}
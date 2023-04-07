#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

using Autodesk.AutoCAD.Geometry;
using AcadGeo = Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcadBRep = Autodesk.AutoCAD.BoundaryRepresentation;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Speckle.Core.Models;

using Objects.Utils;
using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Brep = Objects.Geometry.Brep;
using BrepEdge = Objects.Geometry.BrepEdge;
using BrepFace = Objects.Geometry.BrepFace;
using BrepLoop = Objects.Geometry.BrepLoop;
using BrepLoopType = Objects.Geometry.BrepLoopType;
using BrepTrim = Objects.Geometry.BrepTrim;
using Circle = Objects.Geometry.Circle;
using ControlPoint = Objects.Geometry.ControlPoint;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Spiral = Objects.Geometry.Spiral;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;
using Speckle.Core.Kits;
using Objects.Geometry;

using MathNet.Spatial.Euclidean;
using Objects.Primitive;

using ASPolyline3d = Autodesk.AdvanceSteel.Geometry.Polyline3d;
using ASCurve3d = Autodesk.AdvanceSteel.Geometry.Curve3d;
using ASLineSeg3d = Autodesk.AdvanceSteel.Geometry.LineSeg3d;
using ASCircArc3d = Autodesk.AdvanceSteel.Geometry.CircArc3d;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using ASVector3d = Autodesk.AdvanceSteel.Geometry.Vector3d;
using ASExtents = Autodesk.AdvanceSteel.Geometry.Extents;
using ASPlane = Autodesk.AdvanceSteel.Geometry.Plane;
using ASBoundBlock3d = Autodesk.AdvanceSteel.Geometry.BoundBlock3d;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    private Point PointToSpeckle(ASPoint3d point, string units = null)
    {
      //TODO: handle units.none?
      var u = units ?? ModelUnits;
      var extPt = ToExternalCoordinates(PointASToAcad(point));
      return new Point(extPt.X, extPt.Y, extPt.Z, u);
    }

    private Point3d PointASToAcad(ASPoint3d point)
    {
      return new Point3d(point.x * Factor, point.y * Factor, point.z * Factor);
    }

    private Point3D PointToMath(ASPoint3d point)
    {
      return new Point3D(point.x * Factor, point.y * Factor, point.z * Factor);
    }

    public Vector VectorToSpeckle(ASVector3d vector, string units = null)
    {
      var u = units ?? ModelUnits;
      var extV = ToExternalCoordinates(VectorASToAcad(vector));
      return new Vector(extV.X, extV.Y, extV.Z, ModelUnits);
    }
    private Vector3d VectorASToAcad(ASVector3d vector)
    {
      return new Vector3d(vector.x * Factor, vector.y * Factor, vector.z * Factor);
    }

    private Box BoxToSpeckle(ASBoundBlock3d bound)
    {
      try
      {
        bound.GetMinMaxPoints(out var point1, out var point2);
        // convert min and max pts to speckle first
        var min = PointToSpeckle(point1);
        var max = PointToSpeckle(point2);

        // get dimension intervals
        var xSize = new Interval(min.x, max.x);
        var ySize = new Interval(min.y, max.y);
        var zSize = new Interval(min.z, max.z);

        // get the base plane of the bounding box from extents and current UCS
        var ucs = Doc.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
        var plane = new AcadGeo.Plane(PointASToAcad(point1), ucs.Xaxis, ucs.Yaxis);

        var box = new Box()
        {
          xSize = xSize,
          ySize = ySize,
          zSize = zSize,
          basePlane = PlaneToSpeckle(plane),
          volume = xSize.Length * ySize.Length * zSize.Length,
          units = ModelUnits
        };

        return box;
      }
      catch
      {
        return null;
      }
    }
    private Box BoxToSpeckle(ASExtents extents)
    {
      try
      {
        // convert min and max pts to speckle first
        var min = PointToSpeckle(extents.MinPoint);
        var max = PointToSpeckle(extents.MaxPoint);

        // get dimension intervals
        var xSize = new Interval(min.x, max.x);
        var ySize = new Interval(min.y, max.y);
        var zSize = new Interval(min.z, max.z);

        // get the base plane of the bounding box from extents and current UCS
        var ucs = Doc.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
        var plane = new AcadGeo.Plane(PointASToAcad(extents.MinPoint), ucs.Xaxis, ucs.Yaxis);

        var box = new Box()
        {
          xSize = xSize,
          ySize = ySize,
          zSize = zSize,
          basePlane = PlaneToSpeckle(plane),
          volume = xSize.Length * ySize.Length * zSize.Length,
          units = ModelUnits
        };

        return box;
      }
      catch
      {
        return null;
      }
    }

    private Polycurve PolycurveToSpeckle(ASPolyline3d polyline)
    {
      var units = ModelUnits;
      Polycurve specklePolycurve = new Polycurve(units) { closed = polyline.IsClosed };

      polyline.GetCurves(out ASCurve3d[] foundPolyCurves);
      for (int i = 0; i < foundPolyCurves.Length; i++)
      {
        ASCurve3d nextCurve = foundPolyCurves[i];
        if (nextCurve is ASLineSeg3d line)
        {
          specklePolycurve.segments.Add(LineToSpeckle(line));
        }

        if (nextCurve is ASCircArc3d arc)
        {
          specklePolycurve.segments.Add(ArcToSpeckle(arc));
        }
      }
      return specklePolycurve;
    }

    private Polycurve PolycurveToSpeckle(ASPoint3d[] pointsContour)
    {
      var units = ModelUnits;
      var specklePolycurve = new Polycurve(units);

      for (int i = 1; i < pointsContour.Length; i++)
      {
        specklePolycurve.segments.Add(LineToSpeckle(pointsContour[i - 1], pointsContour[i]));
      }

      specklePolycurve.segments.Add(LineToSpeckle(pointsContour.Last(), pointsContour.First()));

      return specklePolycurve;
    }

    private Line LineToSpeckle(ASPoint3d point1, ASPoint3d point2)
    {
      return new Line(PointToSpeckle(point1), PointToSpeckle(point2), ModelUnits);
    }

    private Line LineToSpeckle(ASLineSeg3d line)
    {
      var _line = new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), ModelUnits);
      _line.length = line.GetLength();

      if (line.HasStartParam(out var start) && line.HasEndParam(out var end))
      {
        _line.domain = new Interval(start, end);
      }

      _line.bbox = BoxToSpeckle(line.GetOrthoBoundBlock());
      return _line;
    }

    private Arc ArcToSpeckle(ASCircArc3d arc)
    {
      Arc _arc;

      if (arc.IsPlanar(out var plane))
      {
        _arc = new Arc(PlaneToSpeckle(plane), PointToSpeckle(arc.StartPoint), PointToSpeckle(arc.EndPoint), arc.IncludedAngle, ModelUnits);
      }
      else
      {
        _arc = new Arc(PointToSpeckle(arc.StartPoint), PointToSpeckle(arc.EndPoint), arc.IncludedAngle, ModelUnits);
      }

      _arc.midPoint = PointToSpeckle(arc.MidPoint);

      if (arc.HasStartParam(out var start) && arc.HasEndParam(out var end))
      {
        _arc.domain = new Interval(start, end);
      }

      _arc.length = arc.GetLength();
      _arc.bbox = BoxToSpeckle(arc.GetOrthoBoundBlock());
      return _arc;
    }

    private Plane PlaneToSpeckle(ASPlane plane)
    {
      plane.GetCoordSystem(out var origin, out var vectorX, out var vectorY, out var vectorZ);

      return new Plane(PointToSpeckle(origin), VectorToSpeckle(plane.Normal), VectorToSpeckle(vectorX), VectorToSpeckle(vectorY), ModelUnits);
    }

  }
}
#endif

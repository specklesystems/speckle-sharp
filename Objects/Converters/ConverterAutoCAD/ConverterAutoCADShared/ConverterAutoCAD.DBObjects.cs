using Speckle.Core.Models;
using Speckle.Core.Kits;
using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AC = Autodesk.AutoCAD.DatabaseServices;

using Arc = Objects.Geometry.Arc;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Autodesk.AutoCAD.ApplicationServices;

namespace Objects.Converter.AutoCAD
{
  public partial class ConverterAutoCAD
  {
    // Points
    public Point PointToSpeckle(DBPoint point)
    {
      return PointToSpeckle(point.Position);
    }
    public DBPoint PointToNativeDB(Point point)
    {
      var _point = new DBPoint(PointToNative(point));
      return _point;
    }

    // Lines
    public Line LineToSpeckle(AC.Line line)
    {
      return new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint));
    }
    public AC.Line LineToNativeDB(Line line)
    {
      return new AC.Line(PointToNative(line.start), PointToNative(line.end));
    }

    // Arcs
    public Arc ArcToSpeckle(AC.Arc arc)
    {
      var _arc = new Arc(PlaneToSpeckle(arc.GetPlane()), arc.Radius, arc.StartAngle, arc.EndAngle, arc.TotalAngle, ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.domain = new Interval(0, 1);
      return _arc;
    }
    public AC.Arc ArcToNativeDB(Arc arc)
    {
      var center = PointToNative(arc.plane.origin);
      var radius = ScaleToNative((double)arc.radius, arc.units);
      var _arc = new AC.Arc(center, radius, (double)arc.startAngle, (double)arc.endAngle);
      return _arc;
    }

    // Circles
    public Circle CircleToSpeckle(AC.Circle circle)
    {
      return new Circle(PlaneToSpeckle(circle.GetPlane()), circle.Radius, ModelUnits);
    }
    public AC.Circle CircleToNativeDB(Circle circle)
    {
      var normal = VectorToNative(circle.plane.normal);
      var radius = ScaleToNative((double)circle.radius, circle.units);
      var _circle = new AC.Circle(PointToNative(circle.center), normal, radius);
      return _circle;
    }

    // Ellipses
    public Ellipse EllipseToSpeckle(AC.Ellipse ellipse)
    {
      var _ellipse = new Ellipse(PlaneToSpeckle(ellipse.GetPlane()), ellipse.MajorRadius, ellipse.MinorRadius, ModelUnits);
      _ellipse.domain = new Interval(0, 1);
      return _ellipse;
    }
    public AC.Ellipse EllipseToNativeDB(Ellipse ellipse)
    {
      var normal = VectorToNative(ellipse.plane.normal);
      var majorAxis = VectorToNative(ellipse.plane.xdir);
      var radiusRatio = (double)ellipse.firstRadius / (double)ellipse.secondRadius;
      var _ellipse = new AC.Ellipse(PointToNative(ellipse.center), normal, majorAxis, radiusRatio, 0, 0); // TODO: solve start and end angle issues
      return _ellipse;
    }

    // Rectangles
    public Polyline RectangleToSpeckle(Rectangle3d rectangle)
    {
      List<Point3d> vertices = new List<Point3d>() { rectangle.LowerLeft, rectangle.LowerRight, rectangle.UpperLeft, rectangle.UpperRight };
      return new Polyline(PointsToFlatArray(vertices), ModelUnits) { closed = true };
    }

    // Splines 
    public Curve SplineToSpeckle(Spline spline)
    {
      var curve = new Curve();
      var nurbs = spline.NurbsData;

      // get control points
      var points = new List<Point3d>();
      foreach (var point in nurbs.GetControlPoints())
        points.Add((Point3d)point);

      // set nurbs curve info
      curve.weights = nurbs.GetWeights().ToArray().ToList();
      curve.points = PointsToFlatArray(points).ToList();
      curve.knots = nurbs.GetKnots().ToArray().ToList();
      curve.degree = nurbs.Degree;
      curve.periodic = nurbs.Periodic;
      curve.rational = nurbs.Rational;
      curve.closed = nurbs.Closed;

      return curve;
    }
    public Spline NurbsToNativeDB(Curve curve)
    {
      var points = new Point3dCollection(PointListToNative(curve.points, curve.units));
      var spline = new Spline(points, curve.degree, tolerance);

      // add knots and weights
      foreach (var knot in curve.knots)
        spline.InsertKnot(knot);
      for (int i = 0; i < curve.weights.Count; i++)
        spline.SetWeightAt(i, curve.weights[i]);

      return spline;
    }
    

    // PolyCurves
    public Polyline PolylineToSpeckle(AC.Polyline polyline) // AC polylines can have arc segments, this treats all segments as lines
    {
      bool isClosed = polyline.Closed;
      List<Point3d> vertices = new List<Point3d>();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
        vertices.Add(polyline.GetPoint3dAt(i));
      return new Polyline(PointsToFlatArray(vertices), ModelUnits) { closed = isClosed };
    }
    public Polyline3d PolylineToNativeDB(Polyline polyline) // Polyline3d does NOT support curves
    {
      var vertices = new Point3dCollection();
      for (int i = 0; i < polyline.points.Count; i++)
        vertices.Add(PointToNative(polyline.points[i]));
      Polyline3d _polyline = new Polyline3d(Poly3dType.SimplePoly, vertices, polyline.closed);
      return _polyline;
    }
    public Polycurve PolycurveToSpeckle(Polyline2d polyline) // AC polyline2d are really polycurves with linear, circlular, or elliptical segments!
    {
      var polycurve = new Polycurve();
      polycurve.closed = polyline.Closed;

      // extract segment curves
      var segments = new List<ICurve>();
      var exploded = new DBObjectCollection();
      polyline.Explode(exploded);
      for(int i = 0; i < exploded.Count; i++)
        segments.Add((ICurve)ConvertToSpeckle(exploded[i]));

      polycurve.segments = segments;
      return polycurve;
    }
    public Polycurve PolycurveToSpeckle(AC.Polyline polyline) // AC polylines are really polycurves with linear, circlular, or elliptical segments!
    {
      var polycurve = new Polycurve();
      polycurve.closed = polyline.Closed;

      // extract each segment type
      var segments = new List<ICurve>();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
      {
        SegmentType type = polyline.GetSegmentType(i);
        switch (type)
        {
          case SegmentType.Line:
            segments.Add(LineToSpeckle(polyline.GetLineSegmentAt(i)));
            break;
          case SegmentType.Arc:
            segments.Add(ArcToSpeckle(polyline.GetArcSegmentAt(i)));
            break;
        }
      }
      polycurve.segments = segments;
      return polycurve;
    }

    public AC.Polyline PolycurveToNativeDB(Polycurve polycurve) //polylines can only support curve segments of type circular arc
    {
      AC.Polyline polyline = new AC.Polyline();
      var plane = new Autodesk.AutoCAD.Geometry.Plane(Point3d.Origin, Vector3d.ZAxis.TransformBy(Doc.Editor.CurrentUserCoordinateSystem)); // TODO: check this 

      // add all vertices
      for (int i = 0; i < polycurve.segments.Count; i++)
      {
        var segment = polycurve.segments[i];
        switch (segment)
        {
          case Line o:
            polyline.AddVertexAt(i, PointToNative(o.start).Convert2d(plane), 0, 0, 0);
            if (!polycurve.closed && i == polycurve.segments.Count - 1)
              polyline.AddVertexAt(i+1, PointToNative(o.end).Convert2d(plane), 0, 0, 0);
            break;
          case Arc o:
            polyline.AddVertexAt(i, PointToNative(o.startPoint).Convert2d(plane), 0, 0, 0);
            polyline.SetBulgeAt(i, Math.Tan((double)(o.endAngle - o.startAngle) / 4)); // bulge defined as tan(quarter total angle)
            if (!polycurve.closed && i == polycurve.segments.Count - 1)
              polyline.AddVertexAt(i+1, PointToNative(o.endPoint).Convert2d(plane), 0, 0, 0);
            break;
          default:
            throw new Exception("Polycurve segment is not a line or arc!");
        }
      }

      // check for closure
      if (polycurve.closed)
        polyline.Closed = true;

      return polyline;
    }


    // Curves
    public AC.Curve CurveToNativeDB(ICurve curve)
    {
      switch (curve)
      {
        case Circle circle:
          return CircleToNativeDB(circle);

        case Arc arc:
          return ArcToNativeDB(arc);

        case Ellipse ellipse:
          return EllipseToNativeDB(ellipse);

        case Curve crv:
          return NurbsToNativeDB(crv);

        case Polyline polyline:
          return PolylineToNativeDB(polyline);

        case Line line:
          return LineToNativeDB(line);

        case Polycurve polycurve:
          return PolycurveToNativeDB(polycurve);

        default:
          return null;
      }
    }
  }
}
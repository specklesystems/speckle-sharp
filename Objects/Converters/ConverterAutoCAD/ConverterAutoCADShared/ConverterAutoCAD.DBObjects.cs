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
      return new DBPoint(PointToNative(point));
    }

    // Lines
    public Line LineToSpeckle(AC.Line line)
    {
      return new Line(PointsToFlatArray(new Point3d[] { line.StartPoint, line.EndPoint }), ModelUnits);
    }
    public AC.Line LineToNativeDB(Line line)
    {
      var pts = PointListToNative(line.value, line.units);
      var _line = new AC.Line(pts[0], pts[1]);
      return _line;
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
      _arc.StartPoint = PointToNative(arc.startPoint);
      _arc.EndPoint = PointToNative(arc.endPoint);
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
    public Polyline PolylineToSpeckle(AC.Polyline polyline) // AC polylines can have curved segments, this is the method will convert all segments to lines
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

      // add polyline to document block table record: this is necessary before adding vertices
      AddObjectToBlockTableRecord(_polyline);

      // add vertices
      foreach (Point3d vertex in _polyline)
        _polyline.AppendVertex(new PolylineVertex3d(vertex));

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

    public AC.Polyline PolycurveToNativeDB(Polycurve polycurve)
    {
      AC.Polyline polyline = new AC.Polyline();

      // add polyline to document block table record: this is necessary before adding vertices
      //AddObjectToBlockTableRecord(polyline);

      for (int i = 0; i < polycurve.segments.Count; i++)
      {
        var segment = (AC.Curve)ConvertToNative((Base)polycurve.segments[i]);
        polyline.AddVertexAt(i, segment.StartPoint.Convert2d(segment.GetPlane()), 0, 0, 0); // TODO: calculate bulge
        segment.Dispose();
      }
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


    /// <summary>
    /// Converts a DB Object <see cref="DBObject"/> instance to a Speckle <see cref="Base"/>
    /// </summary>
    /// <param name="obj">DB Object to be converted.</param>
    /// <returns></returns>
    /// <remarks>
    /// faster way but less readable method is to check object class name string: obj.ObjectId.ObjectClass.DxfName
    /// https://spiderinnet1.typepad.com/blog/2012/04/various-ways-to-check-object-types-in-autocad-net.html
    /// </remarks>
    public Base ObjectToSpeckle(DBObject obj)
    {
      switch (obj)
      {
        case DBPoint o:
          return PointToSpeckle(o);
        case AC.Line o:
          return LineToSpeckle(o);
        case AC.Arc o:
          return ArcToSpeckle(o);
        case AC.Circle o:
          return CircleToSpeckle(o);
        case AC.Ellipse o:
          return EllipseToSpeckle(o);
        case AC.Spline o:
          return SplineToSpeckle(o);
        case AC.Polyline o:
          return PolycurveToSpeckle(o);
        case AC.Polyline2d o:
          return PolycurveToSpeckle(o);
        default:
          return null;
      }
    }

    /*
    /// <summary>
    /// Converts a native AC geometry object into a AC DBObject
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <remarks>This is necessary to bake converted Speckle objects to model space</remarks>
    public DBObject NativeToDBObject(object obj)
    {
      switch (obj)
      {
        case Point3d _:
          return new DBPoint((Point3d)obj);
        case Line3d _:
          Line3d line = (Line3d)obj;
          return new AC.Line(line.StartPoint, line.EndPoint);
        case Curve3d _:
          var curve = (Curve3d)obj;
          return AC.Curve.CreateFromGeCurve(curve);
        default:
          return null;
      }
    }
    */
  }
}
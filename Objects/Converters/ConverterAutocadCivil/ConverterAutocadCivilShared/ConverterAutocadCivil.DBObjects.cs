using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Arc = Objects.Geometry.Arc;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Surface = Objects.Geometry.Surface;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
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
    public Line LineToSpeckle(AcadDB.Line line)
    {
      return new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), ModelUnits);
    }
    public AcadDB.Line LineToNativeDB(Line line)
    {
      return new AcadDB.Line(PointToNative(line.start), PointToNative(line.end));
    }

    // Arcs
    public Arc ArcToSpeckle(AcadDB.Arc arc)
    {
      var _arc = new Arc(PlaneToSpeckle(arc.GetPlane()), arc.Radius, arc.StartAngle, arc.EndAngle, arc.TotalAngle, ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.domain = new Interval(0, 1);
      return _arc;
    }
    public AcadDB.Arc ArcToNativeDB(Arc arc)
    {
      var center = PointToNative(arc.plane.origin);
      var radius = ScaleToNative((double)arc.radius, arc.units);
      return new AcadDB.Arc(center, radius, (double)arc.startAngle, (double)arc.endAngle);
    }

    // Circles
    public Circle CircleToSpeckle(AcadDB.Circle circle)
    {
      var _circle = new Circle(PlaneToSpeckle(circle.GetPlane()), circle.Radius, ModelUnits);
      return _circle;

    }
    public AcadDB.Circle CircleToNativeDB(Circle circle)
    {
      var normal = VectorToNative(circle.plane.normal);
      var radius = ScaleToNative((double)circle.radius, circle.units);
      return new AcadDB.Circle(PointToNative(circle.plane.origin), normal, radius);
    }

    // Ellipses
    public Ellipse EllipseToSpeckle(AcadDB.Ellipse ellipse)
    {
      var _ellipse = new Ellipse(PlaneToSpeckle(ellipse.GetPlane()), ellipse.MajorRadius, ellipse.MinorRadius, ModelUnits);
      _ellipse.domain = new Interval(0, 1);
      return _ellipse;
    }
    public AcadDB.Ellipse EllipseToNativeDB(Ellipse ellipse)
    {
      var normal = VectorToNative(ellipse.plane.normal);
      var majorAxis = ScaleToNative((double)ellipse.firstRadius, ellipse.units) * VectorToNative(ellipse.plane.xdir);
      var radiusRatio =(double)ellipse.secondRadius / (double)ellipse.firstRadius;
      return new AcadDB.Ellipse(PointToNative(ellipse.plane.origin), normal, majorAxis, radiusRatio, 0, 2 * Math.PI);
    }

    // Rectangles
    public Polyline RectangleToSpeckle(Rectangle3d rectangle)
    {
      var vertices = new List<Point3d>() { rectangle.LowerLeft, rectangle.LowerRight, rectangle.UpperLeft, rectangle.UpperRight };
      return new Polyline(PointsToFlatArray(vertices), ModelUnits) { closed = true };
    }

    // Polycurves
    public Polyline PolylineToSpeckle(AcadDB.Polyline polyline) // AC polylines can have arc segments, this treats all segments as lines
    {
      List<Point3d> vertices = new List<Point3d>();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
        vertices.Add(polyline.GetPoint3dAt(i));
      if (polyline.Closed)
        vertices.Add(polyline.GetPoint3dAt(0));

      return new Polyline(PointsToFlatArray(vertices), ModelUnits) { closed = polyline.Closed };
    }
    public Polyline3d PolylineToNativeDB(Polyline polyline) // Polyline3d does NOT support curves
    {
      var vertices = new Point3dCollection();
      for (int i = 0; i < polyline.points.Count; i++)
        vertices.Add(PointToNative(polyline.points[i]));
      return new Polyline3d(Poly3dType.SimplePoly, vertices, polyline.closed);
    }
    public Polycurve PolycurveToSpeckle(Polyline2d polyline) // AC polyline2d are really polycurves with linear, circlular, or elliptical segments!
    {
      var polycurve = new Polycurve(units: ModelUnits) { closed = polyline.Closed };

      // extract segment curves
      var segments = new List<ICurve>();
      var exploded = new DBObjectCollection();
      polyline.Explode(exploded);
      for(int i = 0; i < exploded.Count; i++)
        segments.Add((ICurve)ConvertToSpeckle(exploded[i]));
      polycurve.segments = segments;

      return polycurve;
    }
    public Polycurve PolycurveToSpeckle(AcadDB.Polyline polyline) // AC polylines are polycurves with linear or arc segments
    {
      var polycurve = new Polycurve(units: ModelUnits) { closed = polyline.Closed };

      // extract segments
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
    public AcadDB.Polyline PolycurveToNativeDB(Polycurve polycurve) //polylines can only support curve segments of type circular arc
    {
      AcadDB.Polyline polyline = new AcadDB.Polyline() { Closed = polycurve.closed };
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
            var bulge = Math.Tan((double)(o.endAngle - o.startAngle) / 4); // bulge 
            polyline.AddVertexAt(i, PointToNative(o.startPoint).Convert2d(plane), bulge, 0, 0);
            if (!polycurve.closed && i == polycurve.segments.Count - 1)
              polyline.AddVertexAt(i+1, PointToNative(o.endPoint).Convert2d(plane), 0, 0, 0);
            break;
          default:
            throw new Exception("Polycurve segment is not a line or arc!");
        }
      }

      return polyline;
    }

    // Splines
    public Curve SplineToSpeckle(Spline spline)
    {
      return NurbsToSpeckle((NurbCurve3d)spline.GetGeCurve());
    }

    public AcadDB.Curve NurbsToNativeDB(Curve curve)
    {
      // test to see if this is polyline convertible first?
      return AcadDB.Curve.CreateFromGeCurve(NurbcurveToNative(curve));
    }

    // All curves
    public AcadDB.Curve CurveToNativeDB(ICurve icurve)
    {
      switch (icurve)
      {
        case Line line:
          return LineToNativeDB(line);

        case Polyline polyline:
          return PolylineToNativeDB(polyline);

        case Arc arc:
          return ArcToNativeDB(arc);

        case Circle circle:
          return CircleToNativeDB(circle);

        case Ellipse ellipse:
          return EllipseToNativeDB(ellipse);

        case Polycurve polycurve:
          return PolycurveToNativeDB(polycurve);

        case Curve curve:
          return NurbsToNativeDB(curve);

        default:
          return null;
      }
    }

    // Surfaces
    public Surface SurfaceToSpeckle(AcadDB.Surface surface)
    {
      return null;
    }
    public AcadDB.Surface SurfaceToNativeDB(Surface surface)
    {
      return null;
    }

  }
}
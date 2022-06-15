using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

using Autodesk.AutoCAD.Geometry;
using AcadGeo = Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Objects.Utils;
using AcadBRep = Autodesk.AutoCAD.BoundaryRepresentation;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

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

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // tolerance for geometry:
    public double tolerance = 0.000;

#region Convenience methods
    public List<double> PointsToFlatList(IEnumerable<Point3d> points)
    {
      return points.SelectMany(p => new double[] { p.X, p.Y, p.Z }).ToList();
    }
    public List<double> PointsToFlatList(IEnumerable<Point2d> points)
    {
      return points.SelectMany(p => new double[] { p.X, p.Y, 0 }).ToList();
    }
#endregion

    // Points
    public Point PointToSpeckle(Point3d point, string units = null)
    {
      var u = units ?? ModelUnits;
      return new Point(point.X, point.Y, point.Z, u);
    }
    public Point PointToSpeckle(Point2d point, string units = null)
    {
      var u = units ?? ModelUnits;
      return new Point(point.X, point.Y, 0, u);
    }
    public Point3d PointToNative(Point point)
    {
      var _point = new Point3d(ScaleToNative(point.x, point.units),
        ScaleToNative(point.y, point.units),
        ScaleToNative(point.z, point.units));
      return _point;
    }
    public List<List<ControlPoint>> ControlPointsToSpeckle(AcadGeo.NurbSurface surface, string units = null)
    {
      var u = units ?? ModelUnits;

      var points = new List<List<ControlPoint>>();
      int count = 0;
      for (var i = 0; i < surface.NumControlPointsInU; i++)
      {
        var row = new List<ControlPoint>();
        for (var j = 0; j < surface.NumControlPointsInV; j++)
        {
          var point = surface.ControlPoints[count];
          double weight = 1;
          try
          {
            weight = surface.Weights[count];
          }
          catch { }
          row.Add(new ControlPoint(point.X, point.Y, point.Z, weight, u));
          count++;
        }
        points.Add(row);
      }
      return points;
    }
    public Point PointToSpeckle(DBPoint point, string units = null)
    {
      var u = units ?? ModelUnits;
      return PointToSpeckle(point.Position, u);
    }
    public DBPoint PointToNativeDB(Point point)
    {
      return new DBPoint(PointToNative(point));
    }
    public List<List<ControlPoint>> ControlPointsToSpeckle(AcadDB.NurbSurface surface)
    {
      var points = new List<List<ControlPoint>>();
      for (var i = 0; i < surface.NumberOfControlPointsInU; i++)
      {
        var row = new List<ControlPoint>();
        for (var j = 0; j < surface.NumberOfControlPointsInV; j++)
        {
          var point = surface.GetControlPointAt(i, j);
          var weight = surface.GetWeight(i, j);
          row.Add(new ControlPoint(point.X, point.Y, point.Z, weight, ModelUnits));
        }
        points.Add(row);
      }
      return points;
    }

    // Vectors
    public Vector VectorToSpeckle(Vector3d vector)
    {
      return new Vector(vector.X, vector.Y, vector.Z, ModelUnits);
    }
    public Vector3d VectorToNative(Vector vector)
    {
      return new Vector3d(
        ScaleToNative(vector.x, vector.units),
        ScaleToNative(vector.y, vector.units),
        ScaleToNative(vector.z, vector.units));
    }

    // Interval
    public Interval IntervalToSpeckle(AcadGeo.Interval interval)
    {
      return new Interval(interval.LowerBound, interval.UpperBound);
    }
    public AcadGeo.Interval IntervalToNative(Interval interval)
    {
      return new AcadGeo.Interval((double)interval.start, (double)interval.end, tolerance);
    }

    // Plane 
    public Plane PlaneToSpeckle(AcadGeo.Plane plane)
    {
      Vector xAxis = VectorToSpeckle(plane.GetCoordinateSystem().Xaxis);
      Vector yAxis = VectorToSpeckle(plane.GetCoordinateSystem().Yaxis);
      var _plane = new Plane(PointToSpeckle(plane.PointOnPlane), VectorToSpeckle(plane.Normal), xAxis, yAxis, ModelUnits);
      return _plane;
    }
    public AcadGeo.Plane PlaneToNative(Plane plane)
    {
      return new AcadGeo.Plane(PointToNative(plane.origin), VectorToNative(plane.normal));
    }

    // Line
    public Line LineToSpeckle(LineSegment2d line)
    {
      var _line = new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), ModelUnits);
      _line.length = line.Length;
      _line.domain = IntervalToSpeckle(line.GetInterval());
      return _line;
    }
    public Line LineToSpeckle(Line3d line, string units = null)
    {
      var u = units ?? ModelUnits;

      var startParam = line.GetParameterOf(line.StartPoint);
      var endParam = line.GetParameterOf(line.EndPoint);
      var _line = new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), u);
      _line.length = line.GetLength(startParam, endParam, tolerance);
      _line.domain = IntervalToSpeckle(line.GetInterval());
      _line.bbox = BoxToSpeckle(line.OrthoBoundBlock);

      return _line;
    }
    public Line LineToSpeckle(LineSegment3d line)
    {
      var _line = new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), ModelUnits);
      _line.length = line.Length;
      _line.domain = IntervalToSpeckle(line.GetInterval());
      _line.bbox = BoxToSpeckle(line.OrthoBoundBlock);
      return _line;
    }
    public Line3d LineToNative(Line line)
    {
      var _line = new Line3d(PointToNative(line.start), PointToNative(line.end));
      if (line.domain != null)
        _line.SetInterval(IntervalToNative(line.domain));
      return _line;
    }
    public Line LineToSpeckle(AcadDB.Line line, string units = null)
    {
      var u = units ?? ModelUnits;

      var _line = new Line(PointToSpeckle(line.StartPoint, u), PointToSpeckle(line.EndPoint, u), u);
      _line.domain = new Interval(line.StartParam, line.EndParam);
      _line.length = line.Length;
      _line.bbox = BoxToSpeckle(line.GeometricExtents, true);
      return _line;
    }
    public AcadDB.Line LineToNativeDB(Line line)
    {
      return new AcadDB.Line(PointToNative(line.start), PointToNative(line.end));
    }

    // Rectangle
    public Polyline RectangleToSpeckle(Rectangle3d rectangle)
    {
      var vertices = new List<Point3d>() { rectangle.LowerLeft, rectangle.LowerRight, rectangle.UpperLeft, rectangle.UpperRight };
      return new Polyline(PointsToFlatList(vertices), ModelUnits) { closed = true };
    }

    // Arc
    public Arc ArcToSpeckle(CircularArc2d arc)
    {
      var interval = arc.GetInterval();

      // find arc plane (normal is in clockwise dir)
      var center3 = new Point3d(arc.Center.X, arc.Center.Y, 0);
      AcadGeo.Plane plane = (arc.IsClockWise) ? new AcadGeo.Plane(center3, Vector3d.ZAxis.MultiplyBy(-1)) : new AcadGeo.Plane(center3, Vector3d.ZAxis);

      // calculate total angle. TODO: This needs to be validated across all possible arc orientations
      var totalAngle = (arc.IsClockWise) ? System.Math.Abs(arc.EndAngle - arc.StartAngle) : System.Math.Abs(arc.EndAngle - arc.StartAngle);

      // create arc
      var _arc = new Arc(PlaneToSpeckle(plane), arc.Radius, arc.StartAngle, arc.EndAngle, totalAngle, ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.midPoint = PointToSpeckle(arc.EvaluatePoint((interval.UpperBound - interval.LowerBound) / 2));
      _arc.domain = IntervalToSpeckle(interval);
      _arc.length = arc.GetLength(arc.GetParameterOf(arc.StartPoint), arc.GetParameterOf(arc.EndPoint));
      _arc.bbox = BoxToSpeckle(arc.BoundBlock);
      return _arc;
    }
    public Arc ArcToSpeckle(CircularArc3d arc)
    {
      var interval = arc.GetInterval();
      var _arc = new Arc(PlaneToSpeckle(arc.GetPlane()), arc.Radius, arc.StartAngle, arc.EndAngle, Math.Abs(arc.EndAngle - arc.StartAngle), ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.midPoint = PointToSpeckle(arc.EvaluatePoint((interval.UpperBound - interval.LowerBound) / 2));
      _arc.domain = IntervalToSpeckle(arc.GetInterval());
      _arc.length = arc.GetLength(arc.GetParameterOf(arc.StartPoint), arc.GetParameterOf(arc.EndPoint), tolerance);
      _arc.bbox = BoxToSpeckle(arc.OrthoBoundBlock);
      return _arc;
    }
    public CircularArc3d ArcToNative(Arc arc)
    {
      var _arc = new CircularArc3d(PointToNative(arc.startPoint), PointToNative(arc.midPoint), PointToNative(arc.endPoint));

      _arc.SetAxes(VectorToNative(arc.plane.normal), VectorToNative(arc.plane.xdir));
      _arc.SetAngles((double)arc.startAngle, (double)arc.endAngle);

      return _arc;
    }
    public Arc ArcToSpeckle(AcadDB.Arc arc)
    {
      var _arc = new Arc(PlaneToSpeckle(arc.GetPlane()), arc.Radius, arc.StartAngle, arc.EndAngle, arc.TotalAngle, ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.midPoint = PointToSpeckle(arc.GetPointAtDist(arc.Length / 2));
      _arc.domain = new Interval(arc.StartParam, arc.EndParam);
      _arc.length = arc.Length;
      _arc.bbox = BoxToSpeckle(arc.GeometricExtents, true);
      return _arc;
    }
    public AcadDB.Arc ArcToNativeDB(Arc arc)
    {
      // because of different plane & start/end angle conventions, most reliable method to convert to autocad convention is to calculate from start, end, and midpoint
      var circularArc = ArcToNative(arc);

      // calculate adjusted start and end angles from circularArc reference
      double angle = circularArc.ReferenceVector.AngleOnPlane(PlaneToNative(arc.plane));
      double startAngle = circularArc.StartAngle + angle;
      double endAngle = circularArc.EndAngle + angle;

      var _arc = new AcadDB.Arc(
        PointToNative(arc.plane.origin),
        VectorToNative(arc.plane.normal),
        ScaleToNative((double)arc.radius, arc.units),
        startAngle,
        endAngle);

      return _arc;
    }

    // Circle
    public Circle CircleToSpeckle(AcadDB.Circle circle)
    {
      var _circle = new Circle(PlaneToSpeckle(circle.GetPlane()), circle.Radius, ModelUnits);
      _circle.length = circle.Circumference;
      _circle.bbox = BoxToSpeckle(circle.GeometricExtents, true);
      return _circle;
    }
    public AcadDB.Circle CircleToNativeDB(Circle circle)
    {
      var normal = VectorToNative(circle.plane.normal);
      var radius = ScaleToNative((double)circle.radius, circle.units);
      return new AcadDB.Circle(PointToNative(circle.plane.origin), normal, radius);
    }

    // Ellipse
    public Ellipse EllipseToSpeckle(AcadDB.Ellipse ellipse)
    {
      var plane = new AcadGeo.Plane(ellipse.Center, ellipse.MajorAxis, ellipse.MinorAxis);
      var _ellipse = new Ellipse(PlaneToSpeckle(plane), ellipse.MajorRadius, ellipse.MinorRadius, ModelUnits);
      _ellipse.domain = new Interval(ellipse.StartParam, ellipse.EndParam);
      _ellipse.length = ellipse.GetDistanceAtParameter(ellipse.EndParam);
      _ellipse.bbox = BoxToSpeckle(ellipse.GeometricExtents, true);
      return _ellipse;
    }
    public AcadDB.Ellipse EllipseToNativeDB(Ellipse ellipse)
    {
      var normal = VectorToNative(ellipse.plane.normal);
      var xAxisVector = VectorToNative(ellipse.plane.xdir);
      var majorAxis = ScaleToNative((double)ellipse.firstRadius, ellipse.units) * xAxisVector.GetNormal();
      var radiusRatio = (double)ellipse.secondRadius / (double)ellipse.firstRadius;
      return new AcadDB.Ellipse(PointToNative(ellipse.plane.origin), normal, majorAxis, radiusRatio, 0, 2 * Math.PI);
    }

    // Spiral

    // Polyline
    private Polyline PolylineToSpeckle(Point3dCollection points, bool closed)
    {
      double length = 0;
      List<Point3d> vertices = new List<Point3d>();
      foreach (Point3d point in points)
      {
        if (vertices.Count != 0) length += point.DistanceTo(vertices.Last());
        vertices.Add(point);
      }

      var _polyline = new Polyline(PointsToFlatList(vertices), ModelUnits);
      _polyline.closed = closed || vertices.First().IsEqualTo(vertices.Last()) ? true : false;
      _polyline.length = length;

      return _polyline;
    }
    public Polyline PolylineToSpeckle(AcadDB.Polyline polyline) // AcadDB.Polylines can have linear or arc segments. This will convert to linear
    {
      List<Point3d> vertices = new List<Point3d>();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
        vertices.Add(polyline.GetPoint3dAt(i));

      var _polyline = new Polyline(PointsToFlatList(vertices), ModelUnits);
      _polyline.closed = polyline.Closed || polyline.StartPoint.Equals(polyline.EndPoint) ? true : false; // hatch boundary polylines are not closed, cannot rely on .Closed prop
      _polyline.length = polyline.Length;
      _polyline.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return _polyline;
    }
    public Polyline PolylineToSpeckle(Polyline3d polyline)
    {
      List<Point3d> vertices = new List<Point3d>();

      // if this polyline is a new object, retrieve approximate vertices from spline nurbs data (should only be used for curve display value so far)
      if (polyline.IsNewObject)
      {
        foreach (Point3d vertex in polyline.Spline.NurbsData.GetControlPoints())
          vertices.Add(vertex);
      }
      // otherwise retrieve actual vertices from transaction
      else
      {
        foreach (ObjectId id in polyline)
        {
          var vertex = (PolylineVertex3d)Trans.GetObject(id, OpenMode.ForRead);
          vertices.Add(vertex.Position);
        }
      }

      var _polyline = new Polyline(PointsToFlatList(vertices), ModelUnits);
      _polyline.closed = polyline.Closed || polyline.StartPoint.Equals(polyline.EndPoint) ? true : false;
      _polyline.length = polyline.Length;
      _polyline.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return _polyline;
    }
    public Polyline3d PolylineToNativeDB(Polyline polyline)
    {
      var vertices = new Point3dCollection();
      for (int i = 0; i < polyline.points.Count; i++)
        vertices.Add(PointToNative(polyline.points[i]));
      return new Polyline3d(Poly3dType.SimplePoly, vertices, polyline.closed);
    }

    // Polycurve
    public Polycurve PolycurveToSpeckle(Polyline2d polyline) // AC polyline2d can have linear, circlular, or elliptical segments
    {
      var polycurve = new Polycurve(units: ModelUnits) { closed = polyline.Closed };

      // extract segment curves
      var segments = new List<ICurve>();
      var exploded = new DBObjectCollection();
      polyline.Explode(exploded);
      Point3d previousPoint = new Point3d();
      for (int i = 0; i < exploded.Count; i++)
      {
        var segment = exploded[i] as AcadDB.Curve;

        if (i == 0 && exploded.Count > 1)
        {
          // get the connection point to the next segment - this is necessary since imported polycurves might have segments in different directions
          var connectionPoint = new Point3d();
          var nextSegment = exploded[i + 1] as AcadDB.Curve;
          if (nextSegment.StartPoint.IsEqualTo(segment.StartPoint) || nextSegment.StartPoint.IsEqualTo(segment.EndPoint))
            connectionPoint = nextSegment.StartPoint;
          else
            connectionPoint = nextSegment.EndPoint;
          previousPoint = connectionPoint;
          segment = GetCorrectSegmentDirection(segment, connectionPoint, true, out Point3d otherPoint);
        }
        else
        {
          segment = GetCorrectSegmentDirection(segment, previousPoint, false, out previousPoint);
        }
        segments.Add(CurveToSpeckle(segment));
      }
      polycurve.segments = segments;

      polycurve.length = polyline.Length;
      polycurve.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return polycurve;
    }
    public Polycurve PolycurveToSpeckle(AcadDB.Polyline polyline) // AC polylines are polycurves with linear or arc segments
    {
      var polycurve = new Polycurve(units: ModelUnits) { closed = polyline.Closed };

      // extract segments
      var segments = new List<ICurve>();
      Point3d previousPoint = new Point3d();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
      {
        var segment = GetSegmentByType(polyline, i);
        if (segment == null)
          continue;
        if (i == 0 && polyline.NumberOfVertices > 1)
        {
          // get the connection point to the next segment
          var connectionPoint = new Point3d();
          var nextSegment = GetSegmentByType(polyline, i + 1);
          if (nextSegment.StartPoint.IsEqualTo(segment.StartPoint) || nextSegment.StartPoint.IsEqualTo(segment.EndPoint))
            connectionPoint = nextSegment.StartPoint;
          else
            connectionPoint = nextSegment.EndPoint;
          previousPoint = connectionPoint;
          segment = GetCorrectSegmentDirection(segment, connectionPoint, true, out Point3d otherPoint);
        }
        else
        {
          segment = GetCorrectSegmentDirection(segment, previousPoint, false, out previousPoint);
        }
        segments.Add(CurveToSpeckle(segment));
      }
      polycurve.segments = segments;

      polycurve.length = polyline.Length;
      polycurve.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return polycurve;
    }
    private Curve3d GetSegmentByType(AcadDB.Polyline polyline, int i)
    {
      SegmentType type = polyline.GetSegmentType(i);
      switch (type)
      {
        case SegmentType.Line:
          return polyline.GetLineSegmentAt(i);
        case SegmentType.Arc:
          return polyline.GetArcSegmentAt(i);
        default:
          return null;
      }
    }
    private AcadDB.Curve GetCorrectSegmentDirection(AcadDB.Curve segment, Point3d connectionPoint, bool isFirstSegment, out Point3d nextPoint) // note sometimes curve3d may not have endpoints
    {
      nextPoint = segment.EndPoint;

      if (connectionPoint == null)
        return segment;

      bool reverseDirection = false;
      if (isFirstSegment)
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? true : false;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }
      else
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? false : true;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }

      if (reverseDirection) segment.ReverseCurve();
      return segment;
    }
    private Curve3d GetCorrectSegmentDirection(Curve3d segment, Point3d connectionPoint, bool isFirstSegment, out Point3d nextPoint) // note sometimes curve3d may not have endpoints
    {
      nextPoint = segment.EndPoint;

      if (connectionPoint == null)
        return segment;

      bool reverseDirection = false;
      if (isFirstSegment)
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? true : false;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }
      else
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? false : true;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }

      return (reverseDirection) ? segment.GetReverseParameterCurve() : segment;
    }
    private bool IsPolycurvePlanar(Polycurve polycurve)
    {
      double? z = null;
      foreach (var segment in polycurve.segments)
      {
        switch (segment)
        {
          case Line o:
            if (z == null) z = o.start.z;
            if (o.start.z != z || o.end.z != z) return false;
            break;
          case Arc o:
            if (z == null) z = o.startPoint.z;
            if (o.startPoint.z != z || o.midPoint.z != z || o.endPoint.z != z) return false;
            break;
          case Curve o:
            if (z == null) z = o.points[2];
            for (int i = 2; i < o.points.Count; i += 3)
              if (o.points[i] != z) return false;
            break;
          case Spiral o:
            if (z == null) z = o.startPoint.z;
            if (o.startPoint.z != z || o.endPoint.z != z) return false;
            break;
        }
      }
      return true;
    }

    // polylines can only support curve segments of type circular arc.
    public AcadDB.Polyline PolycurveToNativeDB(Polycurve polycurve)
    {
      AcadDB.Polyline polyline = new AcadDB.Polyline() { Closed = polycurve.closed };
      var plane = new Autodesk.AutoCAD.Geometry.Plane(Point3d.Origin, Vector3d.ZAxis.TransformBy(Doc.Editor.CurrentUserCoordinateSystem)); // TODO: check this 

      // add all vertices
      int count = 0;
      foreach(var segment in polycurve.segments)
      {
        switch (segment)
        {
          case Line o:
            polyline.AddVertexAt(count, PointToNative(o.start).Convert2d(plane), 0, 0, 0);
            if (!polycurve.closed && count == polycurve.segments.Count - 1)
              polyline.AddVertexAt(count + 1, PointToNative(o.end).Convert2d(plane), 0, 0, 0);
            count++;
            break;
          case Arc o:
            var angle = o.endAngle - o.startAngle;
            angle = angle < 0 ? angle + 2 * Math.PI : angle;
            var bulge = Math.Tan((double)angle / 4) * BulgeDirection(o.startPoint, o.midPoint, o.endPoint); // bulge
            polyline.AddVertexAt(count, PointToNative(o.startPoint).Convert2d(plane), bulge, 0, 0);
            if (!polycurve.closed && count == polycurve.segments.Count - 1)
              polyline.AddVertexAt(count + 1, PointToNative(o.endPoint).Convert2d(plane), 0, 0, 0);
            count++;
            break;
          case Spiral o:
            var vertices = o.displayValue.GetPoints().Select(p => PointToNative(p)).ToList();
            foreach(var vertex in vertices)
            {
              polyline.AddVertexAt(count, vertex.Convert2d(plane), 0, 0, 0);
              count++;
            }
            break;
          default:
            return null;
        }
      }

      return polyline;
    }
    // calculates bulge direction: (-) clockwise, (+) counterclockwise
    private int BulgeDirection(Point start, Point mid, Point end)
    {
      // get vectors from points
      double[] v1 = new double[] { end.x - start.x, end.y - start.y, end.z - start.z }; // vector from start to end point
      double[] v2 = new double[] { mid.x - start.x, mid.y - start.y, mid.z - start.z }; // vector from start to mid point

      // calculate cross product z direction
      double z = v1[0] * v2[1] - v2[0] * v1[1];

      if (z > 0)
        return -1;
      else
        return 1;
    }

    // Spline
    public Curve SplineToSpeckle(Spline spline)
    {
      var curve = new Curve();

      // get nurbs and geo data 
      var data = spline.NurbsData;
      var _spline = spline.GetGeCurve() as NurbCurve3d;

      // hack: check for incorrectly closed periodic curves (this seems like acad bug, has resulted from receiving rhino curves)
      bool periodicClosed = false;
      if (_spline.Knots.Count < _spline.NumberOfControlPoints + _spline.Degree + 1 && spline.IsPeriodic)
        periodicClosed = true;

      // handle the display polyline
      try
      {
        var poly = spline.ToPolyline(false, true);
        Polyline displayValue = CurveToSpeckle(poly) as Polyline;
        curve.displayValue = displayValue;
      }
      catch { }

      // get points
      // NOTE: for closed periodic splines, autocad does not track last #degree points. Add the first #degree control points to the list if so.
      var points = data.GetControlPoints().OfType<Point3d>().ToList();
      if (periodicClosed)
        points.AddRange(points.GetRange(0, spline.Degree));

      // get knots
      // NOTE: for closed periodic splines, autocad has #control points + 1 knots. Add #degree extra knots to beginning and end with #degree - 1 multiplicity for first and last
      var knots = data.GetKnots().OfType<double>().ToList();
      if (periodicClosed)
      {
        double interval = knots[1] - knots[0]; //knot interval

        for (int i = 0; i < data.Degree; i++)
        {
          if (i < 2)
          {
            knots.Insert(knots.Count, knots[knots.Count - 1] + interval);
            knots.Insert(0, knots[0] - interval);
          }
          else
          {
            knots.Insert(knots.Count, knots[knots.Count - 1]);
            knots.Insert(0, knots[0]);
          }
        }
      }

      // get weights
      // NOTE: autocad assigns unweighted points a value of -1, and will return an empty list in the spline's nurbsdata if no points are weighted
      // NOTE: for closed periodic splines, autocad does not track last #degree points. Add the first #degree weights to the list if so.
      var weights = new List<double>();
      for (int i = 0; i < spline.NumControlPoints; i++)
      {
        double weight = spline.WeightAt(i);
        if (weight <= 0)
          weights.Add(1);
        else
          weights.Add(weight);
      }
      if (periodicClosed)
        weights.AddRange(weights.GetRange(0, spline.Degree));

      // set nurbs curve info
      curve.points = PointsToFlatList(points).ToList();
      curve.knots = knots;
      curve.weights = weights;
      curve.degree = spline.Degree;
      curve.periodic = spline.IsPeriodic;
      curve.rational = spline.IsRational;
      curve.closed = (periodicClosed) ? true : spline.Closed;
      curve.length = _spline.GetLength(_spline.StartParameter, _spline.EndParameter, tolerance);
      curve.domain = IntervalToSpeckle(_spline.GetInterval());
      curve.bbox = BoxToSpeckle(spline.GeometricExtents, true);
      curve.units = ModelUnits;

      return curve;
    }
    // handles polycurves with spline segments: bakes segments individually and then joins
    public AcadDB.Curve PolycurveSplineToNativeDB(Polycurve polycurve)
    {
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();

      Entity first = null;
      List<Entity> others = new List<Entity>();
      for (int i = 0; i < polycurve.segments.Count; i++)
      {
        var converted = CurveToNativeDB(polycurve.segments[i]);
        if (converted == null)
          continue;

        var newEntity = Trans.GetObject(modelSpaceRecord.Append(converted), OpenMode.ForWrite) as Entity;
        if (first == null)
          first = newEntity;
        else
          others.Add(newEntity);
      }

      try
      {
        first.JoinEntities(others.ToArray());
        return first as Spline;
      }
      catch (Exception e)
      {
        Report.ConversionLog.Add("Could not join Polycurve segments: segments converted individually.");
        return null;
      }
    }

    // Curve
    // TODO: NOT TESTED 
    public ICurve CurveToSpeckle(Curve3d curve, string units = null)
    {
      var u = units ?? ModelUnits;

      // note: some curve3ds may not have endpoints! Not sure what contexts this may occur in, might cause issues later.
      switch (curve)
      {
        case LineSegment3d line:
          return LineToSpeckle(line);
        case CircularArc3d arc:
          return ArcToSpeckle(arc);
        default:
          return NurbsToSpeckle(curve as NurbCurve3d);
      }
    }
    public ICurve CurveToSpeckle(Curve2d curve, string units = null)
    {
      var u = units ?? ModelUnits;

      // note: some curve2ds may not have endpoints!
      switch (curve)
      {
        case LineSegment2d line:
          return LineToSpeckle(line);
        case CircularArc2d arc:
          return ArcToSpeckle(arc);
        default:
          return NurbsToSpeckle(curve as NurbCurve2d);
      }
    }
    public Curve NurbsToSpeckle(NurbCurve2d curve)
    {
      var _curve = new Curve();

      // get control points
      var points = new List<Point2d>();
      for (int i = 0; i < curve.NumControlPoints; i++)
        points.Add(curve.GetControlPointAt(i));

      // get knots
      var knots = new List<double>();
      for (int i = 0; i < curve.NumKnots; i++)
        knots.Add(curve.GetKnotAt(i));

      // get weights
      var weights = new List<double>();
      for (int i = 0; i < curve.NumWeights; i++)
        weights.Add(curve.GetWeightAt(i));

      // set nurbs curve info
      _curve.points = PointsToFlatList(points);
      _curve.knots = knots;
      _curve.weights = weights;
      _curve.degree = curve.Degree;
      _curve.periodic = curve.IsPeriodic(out double period);
      _curve.rational = curve.IsRational;
      _curve.closed = curve.IsClosed();
      _curve.length = curve.GetLength(curve.StartParameter, curve.EndParameter);
      _curve.domain = IntervalToSpeckle(curve.GetInterval());
      _curve.units = ModelUnits;

      return _curve;
    }
    public Curve NurbsToSpeckle(NurbCurve3d curve)
    {
      var _curve = new Curve();

      // get control points
      var points = new List<Point3d>();
      for (int i = 0; i < curve.NumberOfControlPoints; i++)
        points.Add(curve.ControlPointAt(i));

      // get knots
      var knots = new List<double>();
      for (int i = 0; i < curve.NumberOfKnots; i++)
        knots.Add(curve.KnotAt(i));

      // get weights
      var weights = new List<double>();
      for (int i = 0; i < curve.NumWeights; i++)
        weights.Add(curve.GetWeightAt(i));

      // set nurbs curve info
      _curve.points = PointsToFlatList(points);
      _curve.knots = knots;
      _curve.weights = weights;
      _curve.degree = curve.Degree;
      _curve.periodic = curve.IsPeriodic(out double period);
      _curve.rational = curve.IsRational;
      _curve.closed = curve.IsClosed();
      _curve.length = curve.GetLength(curve.StartParameter, curve.EndParameter, tolerance);
      _curve.domain = IntervalToSpeckle(curve.GetInterval());
      _curve.bbox = BoxToSpeckle(curve.OrthoBoundBlock);
      _curve.units = ModelUnits;

      return _curve;
    }
    // TODO: need to handle curves generated from polycurves with spline segments (this probably has to do with control points with varying # of knots associated with it)
    public NurbCurve3d NurbcurveToNative(Curve curve)
    {
      // process control points
      // NOTE: for **closed periodic** curves that have "n" control pts, curves sent from rhino will have n+degree points. Remove extra pts for autocad.
      var _points = curve.GetPoints().Select(o => PointToNative(o)).ToList();
      if (curve.closed && curve.periodic)
        _points = _points.GetRange(0, _points.Count - curve.degree);
      var points = new Point3dCollection(_points.ToArray());

      // process knots
      // NOTE: Autocad defines spline knots  as a vector of size # control points + degree + 1. (# at start and end should match degree)
      // Conversions for autocad need to make sure this is satisfied, otherwise will cause protected mem crash.
      // NOTE: for **closed periodic** curves that have "n" control pts, # of knots should be n + 1. Remove degree = 3 knots from start and end.
      var _knots = curve.knots;
      if (curve.knots.Count == _points.Count + curve.degree - 1) // handles rhino format curves
      {
        _knots.Insert(0, _knots[0]);
        _knots.Insert(_knots.Count - 1, _knots[_knots.Count - 1]);
      }
      if (curve.closed && curve.periodic) // handles closed periodic curves
        _knots = _knots.GetRange(curve.degree, _knots.Count - curve.degree * 2);
      var knots = new KnotCollection();
      foreach (var _knot in _knots)
        knots.Add(_knot);

      // process weights
      // NOTE: if all weights are the same, autocad convention is to pass an empty list (this will assign them a value of -1)
      var _weights = curve.weights;
      if (curve.closed && curve.periodic) // handles closed periodic curves
        _weights = curve.weights.GetRange(0, _points.Count);
      DoubleCollection weights;
      weights = (_weights.Distinct().Count() == 1) ? new DoubleCollection() : new DoubleCollection(_weights.ToArray());

      NurbCurve3d _curve = new NurbCurve3d(curve.degree, knots, points, weights, curve.periodic);
      if (curve.closed)
        _curve.MakeClosed();
      _curve.SetInterval(IntervalToNative(curve.domain));

      return _curve;
    }
    public ICurve CurveToSpeckle(AcadDB.Curve curve, string units = null)
    {
      var u = units ?? ModelUnits;

      switch (curve)
      {
        case AcadDB.Line line:
          return LineToSpeckle(line, u);

        case AcadDB.Polyline polyline:
          if (polyline.IsOnlyLines)
            return PolylineToSpeckle(polyline);
          else
            return PolycurveToSpeckle(polyline);

        case AcadDB.Polyline2d polyline2d:
          return PolycurveToSpeckle(polyline2d);

        case AcadDB.Polyline3d polyline3d:
          return PolylineToSpeckle(polyline3d);

        case AcadDB.Arc arc:
          return ArcToSpeckle(arc);

        case AcadDB.Circle circle:
          return CircleToSpeckle(circle);

        case AcadDB.Ellipse ellipse:
          return EllipseToSpeckle(ellipse);

        case AcadDB.Spline spline:
          return SplineToSpeckle(spline);

        default:
          return null;
      }
    }
    public AcadDB.Curve NurbsToNativeDB(Curve curve)
    {
      var _curve = AcadDB.Curve.CreateFromGeCurve(NurbcurveToNative(curve));
      return _curve;
    }
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
          if (polycurve.segments.Where(o => o is Curve).Count() > 0)
            return PolycurveSplineToNativeDB(polycurve);
          return PolycurveToNativeDB(polycurve);

        case Curve curve:
          return NurbsToNativeDB(curve);

        default:
          return null;
      }
    }

    // Surface
    // TODO: NOT TESTED 
    public Surface SurfaceToSpeckle(AcadGeo.NurbSurface surface, string units = null)
    {
      var u = units ?? ModelUnits;

      List<double> Uknots = new List<double>();
      List<double> Vknots = new List<double>();
      foreach (var knot in surface.UKnots)
        Uknots.Add((double)knot);
      foreach (var knot in surface.VKnots)
        Vknots.Add((double)knot);

      var _surface = new Surface()
      {
        degreeU = surface.DegreeInU,
        degreeV = surface.DegreeInV,
        rational = surface.IsRationalInU && surface.IsRationalInV,
        closedU = surface.IsClosedInU(),
        closedV = surface.IsClosedInV(),
        knotsU = Uknots,
        knotsV = Vknots,
        countU = surface.NumControlPointsInU,
        countV = surface.NumControlPointsInV,
        domainU = IntervalToSpeckle(surface.GetEnvelope()[0]),
        domainV = IntervalToSpeckle(surface.GetEnvelope()[1])
      };
      _surface.SetControlPoints(ControlPointsToSpeckle(surface));
      _surface.units = u;

      return _surface;
    }
    // TODO: NOT TESTED 
    public AcadGeo.NurbSurface SurfaceToNative(Surface surface)
    {
      // Get control points
      var points = surface.GetControlPoints().Select(l => l.Select(p =>
        new ControlPoint(
          ScaleToNative(p.x, p.units),
          ScaleToNative(p.y, p.units),
          ScaleToNative(p.z, p.units),
          p.weight,
          p.units)).ToList()).ToList();

      var _surface = AcadGeo.NurbSurface.Create(new IntPtr(), true); // check what new unmanaged pointer does!!

      // Set control points
      Point3dCollection controlPoints = new Point3dCollection();
      DoubleCollection weights = new DoubleCollection();
      for (var i = 0; i < points.Count; i++)
      {
        for (var j = 0; j < points[i].Count; j++)
        {
          var pt = points[i][j];
          controlPoints.Add(PointToNative(pt));
          weights.Add(pt.weight);
        }
      }

      // Get knot vectors
      KnotCollection UKnots = new KnotCollection();
      KnotCollection VKnots = new KnotCollection();
      for (int i = 0; i < surface.knotsU.Count; i++)
        UKnots.Add(surface.knotsU[i]);
      for (int i = 0; i < surface.knotsV.Count; i++)
        VKnots.Add(surface.knotsV[i]);

      // Set surface info
      _surface.Set(surface.degreeU, surface.degreeV, 0, 0, surface.countU, surface.countV, controlPoints, weights, UKnots, VKnots);

      return _surface;
    }
    public Mesh SurfaceToSpeckle(AcadDB.Surface surface, string units = null)
    {
      var u = units ?? ModelUnits;

      switch (surface)
      {
        case AcadDB.PlaneSurface _:
        case AcadDB.NurbSurface _:
        default: // return mesh for now
          var displayMesh = GetMeshFromSolidOrSurface(surface: surface);
          return displayMesh;
      }
    }
    public Surface SurfaceToSpeckle(AcadDB.NurbSurface surface, string units = null)
    {
      var u = units ?? ModelUnits;

      List<double> Uknots = new List<double>();
      List<double> Vknots = new List<double>();
      foreach (var knot in surface.UKnots)
        Uknots.Add((double)knot);
      foreach (var knot in surface.VKnots)
        Vknots.Add((double)knot);

      var _surface = new Surface
      {
        degreeU = surface.DegreeInU,
        degreeV = surface.DegreeInV,
        rational = surface.IsRational,
        closedU = surface.IsClosedInU,
        closedV = surface.IsClosedInV,
        knotsU = Uknots,
        knotsV = Vknots,
        countU = surface.NumberOfControlPointsInU,
        countV = surface.NumberOfControlPointsInV
      };
      _surface.SetControlPoints(ControlPointsToSpeckle(surface));
      _surface.bbox = BoxToSpeckle(surface.GeometricExtents, true);
      _surface.units = ModelUnits;

      return _surface;
    }
    public AcadDB.Surface SurfaceToNativeDB(Surface surface)
    {
      // get control points
      Point3dCollection controlPoints = new Point3dCollection();
      DoubleCollection weights = new DoubleCollection();
      var points = surface.GetControlPoints();
      for (var i = 0; i < points.Count; i++)
      {
        for (var j = 0; j < points[i].Count; j++)
        {
          var pt = points[i][j];
          controlPoints.Add(PointToNative(pt));
          weights.Add(pt.weight);
        }
      }

      // get knots
      var knotsU = new KnotCollection();
      var knotsV = new KnotCollection();
      foreach (var knotU in surface.knotsU)
        knotsU.Add(knotU);
      foreach (var knotV in surface.knotsV)
        knotsV.Add(knotV);

      var _surface = new AcadDB.NurbSurface(
        surface.degreeU,
        surface.degreeV,
        surface.rational,
        surface.countU,
        surface.countV,
        controlPoints,
        weights,
        knotsU,
        knotsV);

      return _surface;
    }

    // Region
    public Mesh RegionToSpeckle(Region region, string units = null)
    {
      return GetMeshFromSolidOrSurface(region: region);
    }

    // Box
    public Box BoxToSpeckle(BoundBlock2d bound, bool OrientToWorldXY = false)
    {
      Box box = null;

      var min3d = new Point3d(bound.GetMinimumPoint().X, bound.GetMinimumPoint().Y, 0);
      var max3d = new Point3d(bound.GetMaximumPoint().X, bound.GetMaximumPoint().Y, 0);

      // get dimension intervals
      var xSize = new Interval(min3d.X, max3d.X);
      var ySize = new Interval(min3d.Y, max3d.Y);
      var zSize = new Interval(min3d.Z, max3d.Z);

      // get box size info
      double area = 2 * ((xSize.Length * ySize.Length) + (xSize.Length * zSize.Length) + (ySize.Length * zSize.Length));
      double volume = xSize.Length * ySize.Length * zSize.Length;

      if (OrientToWorldXY)
      {
        var origin = new Point3d(0, 0, 0);
        var normal = new Vector3d(0, 0, 1);
        var plane = PlaneToSpeckle(new AcadGeo.Plane(origin, normal));
        box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
      }
      else
      {
        // get base plane
        var corner = new Point3d(max3d.X, max3d.Y, min3d.Z);
        var origin = new Point3d((corner.X + min3d.X) / 2, (corner.Y + min3d.Y) / 2, (corner.Z + min3d.Z) / 2);
        var plane = PlaneToSpeckle(new AcadGeo.Plane(min3d, origin, corner));
        box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
      }
      return box;
    }
    public Box BoxToSpeckle(BoundBlock3d bound, bool OrientToWorldXY = false)
    {
      try
      {
        Box box = null;

        var min = bound.GetMinimumPoint();
        var max = bound.GetMaximumPoint();

        // get dimension intervals
        var xSize = new Interval(min.X, max.X);
        var ySize = new Interval(min.Y, max.Y);
        var zSize = new Interval(min.Z, max.Z);

        // get box size info
        double area = 2 * ((xSize.Length * ySize.Length) + (xSize.Length * zSize.Length) + (ySize.Length * zSize.Length));
        double volume = xSize.Length * ySize.Length * zSize.Length;

        if (OrientToWorldXY)
        {
          var origin = new Point3d(0, 0, 0);
          var normal = new Vector3d(0, 0, 1);
          var plane = PlaneToSpeckle(new AcadGeo.Plane(origin, normal));
          box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
        }
        else
        {
          // get base plane
          var corner = new Point3d(max.X, max.Y, min.Z);
          var origin = new Point3d((corner.X + min.X) / 2, (corner.Y + min.Y) / 2, (corner.Z + min.Z) / 2);
          var plane = PlaneToSpeckle(new AcadGeo.Plane(min, origin, corner));
          box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
        }

        return box;
      }
      catch
      {
        return null;
      }
    }
    public Box BoxToSpeckle(AcadDB.Extents3d extents, bool OrientToWorldXY = false)
    {
      try
      {
        Box box = null;

        // get dimension intervals
        var xSize = new Interval(extents.MinPoint.X, extents.MaxPoint.X);
        var ySize = new Interval(extents.MinPoint.Y, extents.MaxPoint.Y);
        var zSize = new Interval(extents.MinPoint.Z, extents.MaxPoint.Z);

        // get box size info
        double area = 2 * ((xSize.Length * ySize.Length) + (xSize.Length * zSize.Length) + (ySize.Length * zSize.Length));
        double volume = xSize.Length * ySize.Length * zSize.Length;

        if (OrientToWorldXY)
        {
          var origin = new Point3d(0, 0, 0);
          var normal = new Vector3d(0, 0, 1);
          var plane = PlaneToSpeckle(new Autodesk.AutoCAD.Geometry.Plane(origin, normal));
          box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
        }
        else
        {
          // get base plane
          var corner = new Point3d(extents.MaxPoint.X, extents.MaxPoint.Y, extents.MinPoint.Z);
          var origin = new Point3d((corner.X + extents.MinPoint.X) / 2, (corner.Y + extents.MinPoint.Y) / 2, (corner.Z + extents.MinPoint.Z) / 2);
          var plane = PlaneToSpeckle(new Autodesk.AutoCAD.Geometry.Plane(extents.MinPoint, origin, corner));
          box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
        }

        return box;
      }
      catch
      {
        return null;
      }
    }

    // Brep
    public Mesh SolidToSpeckle(Solid3d solid, string units = null)
    {
      return GetMeshFromSolidOrSurface(solid: solid);

      /* Not in use currently: needs development on trims
      // make brep
      var brep = new AcadBRep.Brep(solid);
      var t = brep.Faces.First().GetSurfaceAsTrimmedNurbs()[0].GetContours();

      // output lists
      var speckleBrep = new Brep(displayValue: displayMesh, provenance: Applications.Autocad2021, units: u);
      var speckleFaces = new List<BrepFace>();
      var speckleLoops = new List<BrepLoop>();
      var speckleSurfaces = new List<Surface>();
      var speckleTrims = new List<BrepTrim>();
      var speckleEdges = new List<BrepEdge>();
      var SpeckleCurve2ds = new List<ICurve>();

      // process vertices
      var vertexList = brep.Vertices.ToList();
      var speckleVertices = vertexList.Select(o => PointToSpeckle(o.Point, u)).ToList();

      // process faces, surfaces, loops, curve3ds
      var faceList = new List<AcadBRep.Face>();
      var loopList = new List<AcadBRep.BoundaryLoop>();
      var curve3dList = new List<Curve3d>();
      for (int i = 0; i < brep.Faces.Count(); i++)
      {
        var face = brep.Faces.ElementAt(i);
        faceList.Add(face);

        // surfaces
        speckleSurfaces.Add(SurfaceToSpeckle(face.GetSurfaceAsNurb(), u));

        // curve3ds
        var boundaries = face.GetSurfaceAsTrimmedNurbs().First().GetContours();
        foreach (var boundary in boundaries)
          foreach (var contour in boundary.Contour.GetCurve3ds().Select(o => (Curve3d)o))
            if (curve3dList.Where(o => o.IsEqualTo(contour)).Count() == 0)
              curve3dList.Add(contour);

        // loops
        var loops = new List<int>();
        int count = loopList.Count;
        int outerLoop = count;
        foreach (var loop in face.Loops)
        {
          loopList.Add(loop); loops.Add(count);
          if (loop.LoopType == AcadBRep.LoopType.LoopExterior)
            outerLoop = count;
          var speckleLoop = new BrepLoop(speckleBrep, i, null, GetLoopType(loop.LoopType));
          speckleLoops.Add(speckleLoop);
          count++;
        }
        var speckleFace = new BrepFace(speckleBrep, i, loops, outerLoop, !face.IsOrientToSurface);
        speckleFaces.Add(speckleFace);
      }
      var speckleCurve3ds = curve3dList.Select(o => CurveToSpeckle(o)).ToList();

      // process edges
      var edgeDictionary = new Dictionary<AcadBRep.Edge, int>();
      for (int i = 0; i < brep.Edges.Count(); i++)
      {
        var edge = brep.Edges.ElementAt(i);
        edgeDictionary.Add(edge, i);

        var startIndex = GetIndexOfVertex(vertexList, edge.Vertex1);
        var endIndex = GetIndexOfVertex(vertexList, edge.Vertex2);
        var crvIndex = GetIndexOfCurve(curve3dList,edge.Curve);

        var speckleEdge = new BrepEdge(speckleBrep, crvIndex, null, startIndex, endIndex, !edge.IsOrientToCurve, IntervalToSpeckle(edge.Curve.GetInterval()));
        speckleEdges.Add(speckleEdge);
      }

      // set props
      speckleBrep.Curve3D = speckleCurve3ds;
      speckleBrep.Edges = speckleEdges;
      speckleBrep.Faces = speckleFaces;
      speckleBrep.Surfaces = speckleSurfaces;
      speckleBrep.Vertices = speckleVertices;
      speckleBrep.Loops = speckleLoops;

      speckleBrep.IsClosed = true;
      speckleBrep.Orientation = Geometry.BrepOrientation.Unknown;
      speckleBrep.volume = brep.GetVolume();
      speckleBrep.bbox = BoxToSpeckle(brep.BoundBlock);
      speckleBrep.area = brep.GetSurfaceArea();
      return speckleBrep;
      */
    }
    private int GetIndexOfCurve(List<Curve3d> list, Curve3d curve) // necessary since contains comparer doesn't work
    {
      int index = -1;
      for (int i = 0; i < list.Count; i++)
      {
        if (list[i].IsEqualTo(curve))
        {
          index = i;
          break;
        }
      }
      return index;
    }
    private int GetIndexOfVertex(List<AcadBRep.Vertex> list, AcadBRep.Vertex vertex)
    {
      int index = -1;
      for (int i = 0; i < list.Count; i++)
      {
        if (list[i].Point.IsEqualTo(vertex.Point))
        {
          index = i;
          break;
        }
      }
      return index;
    }
    private BrepLoopType GetLoopType(AcadBRep.LoopType loopType)
    {
      switch (loopType)
      {
        case AcadBRep.LoopType.LoopExterior:
          return BrepLoopType.Outer;
        case AcadBRep.LoopType.LoopInterior:
          return BrepLoopType.Inner;
        default:
          return BrepLoopType.Unknown;
      }
    }

    // Mesh
    /* need edge & face info on polygon meshes
    public Mesh MeshToSpeckle(AcadDB.PolygonMesh mesh)
    {
      var _vertices = new List<Point3d>();
      var colors = new List<int>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in mesh)
        {
          var vertex = (PolygonMeshVertex)tr.GetObject(id, OpenMode.ForRead);
          _vertices.Add(vertex.Position);
          colors.Add(vertex.Color.ColorValue.ToArgb());
        }
        tr.Commit();
      }
      var vertices = PointsToFlatArray(_vertices);

      var speckleMesh = new Mesh(vertices, faces, colors.ToArray(), null, ModelUnits);
      speckleMesh.bbox = BoxToSpeckle(mesh.GeometricExtents, true);

      return speckleMesh;
    }
    */
    // Polyface mesh vertex indexing starts at 1. Subtract 1 from face vertex index when sending to Speckle
    public Mesh MeshToSpeckle(PolyFaceMesh mesh, string units = null)
    {
      var u = units ?? ModelUnits;

      var _vertices = new List<Point3d>();
      var faces = new List<int>();
      var colors = new List<int>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in mesh)
        {
          DBObject obj = tr.GetObject(id, OpenMode.ForRead);
          switch (obj)
          {
            case PolyFaceMeshVertex o:
              _vertices.Add(o.Position);
              colors.Add(o.Color.ColorValue.ToArgb());
              break;
            case FaceRecord o:
              var indices = new List<int>();
              for (short i = 0; i < 4; i++)
              {
                short index = o.GetVertexAt(i);
                if (index != 0)
                  indices.Add(index);
              }
              if (indices.Count == 4) // vertex index starts at 1 sigh
                faces.AddRange(new List<int> { 1, indices[0] - 1, indices[1] - 1, indices[2] - 1, indices[3] - 1 });
              else
                faces.AddRange(new List<int> { 0, indices[0] - 1, indices[1] - 1, indices[2] - 1 });
              break;
          }
        }
        tr.Commit();
      }
      var vertices = PointsToFlatList(_vertices);

      var speckleMesh = new Mesh(vertices, faces, colors, null, u);
      speckleMesh.bbox = BoxToSpeckle(mesh.GeometricExtents, true);

      return speckleMesh;
    }
    public Mesh MeshToSpeckle(SubDMesh mesh)
    {
      // vertices
      var _vertices = new List<Point3d>();
      foreach (Point3d point in mesh.Vertices)
        _vertices.Add(point);
      var vertices = PointsToFlatList(_vertices);

      // faces
      var faces = new List<int>();
      int[] faceArr = mesh.FaceArray.ToArray(); // contains vertex indices
      int edgeCount = 0;
      for (int i = 0; i < faceArr.Length; i = i + edgeCount + 1)
      {
        List<int> faceVertices = new List<int>();
        edgeCount = faceArr[i];
        for (int j = i + 1; j <= i + edgeCount; j++)
          faceVertices.Add(faceArr[j]);
        if (edgeCount == 4) // quad face
          faces.AddRange(new List<int> { 1, faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3] });
        else // triangle face
          faces.AddRange(new List<int> { 0, faceVertices[0], faceVertices[1], faceVertices[2] });
      }

      // colors
      var colors = mesh.VertexColorArray.Select(o => Color.FromArgb(Convert.ToInt32(o.Red), Convert.ToInt32(o.Green), Convert.ToInt32(o.Blue)).ToArgb()).ToList();

      var speckleMesh = new Mesh(vertices, faces, colors, null, ModelUnits);
      speckleMesh.bbox = BoxToSpeckle(mesh.GeometricExtents, true);

      return speckleMesh;
    }
    // Polyface mesh vertex indexing starts at 1. Add 1 to face vertex index when converting to native
    public PolyFaceMesh MeshToNativeDB(Mesh mesh)
    {
      mesh.TriangulateMesh(true);
      
      // get vertex points
      var vertices = new Point3dCollection();
      var points = mesh.GetPoints().Select(o => PointToNative(o)).ToList();
      foreach (var point in points)
        vertices.Add(point);

      PolyFaceMesh _mesh = null;

      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        _mesh = new PolyFaceMesh();
        _mesh.SetDatabaseDefaults();

        // append mesh to blocktable record - necessary before adding vertices and faces
        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite);
        btr.AppendEntity(_mesh);
        tr.AddNewlyCreatedDBObject(_mesh, true);

        // add polyfacemesh vertices
        for (int i = 0; i < vertices.Count; i++)
        {
          var vertex = new PolyFaceMeshVertex(points[i]);
          if (mesh.colors.Count > 0)
          {
            try
            {
              Color color = Color.FromArgb(mesh.colors[i]);
              vertex.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(color.R, color.G, color.B);
            }
            catch { }
          }
          if (vertex.IsNewObject)
          {
            _mesh.AppendVertex(vertex);
            tr.AddNewlyCreatedDBObject(vertex, true);
          }
        }

        // add polyfacemesh faces. vertex index starts at 1 sigh
        int j = 0;
        while (j < mesh.faces.Count)
        {
          FaceRecord face;
          if (mesh.faces[j] == 3) // triangle
          {
            face = new FaceRecord((short)(mesh.faces[j + 1] + 1), (short)(mesh.faces[j + 2] + 1), (short)(mesh.faces[j + 3] + 1), 0);
            j += 4;
          }
          else // quad
          {
            face = new FaceRecord((short)(mesh.faces[j + 1] + 1), (short)(mesh.faces[j + 2] + 1), (short)(mesh.faces[j + 3] + 1), (short)(mesh.faces[j + 4] + 1));
            j += 5;
          }
          
          if (face.IsNewObject)
          {
            _mesh.AppendFaceRecord(face);
            tr.AddNewlyCreatedDBObject(face, true);
          }
          
        }

        tr.Commit();
      }

      return _mesh;
    }
    // Based on Kean Walmsley's blog post on mesh conversion using Brep API
    private Mesh GetMeshFromSolidOrSurface(Solid3d solid = null, AcadDB.Surface surface = null, Region region = null)
    {
      Mesh mesh = null;
      double volume = 0;
      double area = 0;

      AcadBRep.Brep brep = null;
      Box bbox = null;
      if (solid != null)
      {
        brep = new AcadBRep.Brep(solid);
        try
        {
          area = solid.Area;
          volume = solid.MassProperties.Volume;
        }
        catch (Exception e)
        { };
        
        bbox = BoxToSpeckle(solid.GeometricExtents);
      }
      else if (surface != null)
      {
        brep = new AcadBRep.Brep(surface);
        area = surface.GetArea();
        bbox = BoxToSpeckle(surface.GeometricExtents);
      }
      else if (region != null)
      {
        brep = new AcadBRep.Brep(region);
        area = region.Area;
        bbox = BoxToSpeckle(region.GeometricExtents);
      }

      if (brep != null)
      {
        try
        {
          using (var control = new AcadBRep.Mesh2dControl())
          {
            // These settings may need adjusting
            control.MaxSubdivisions = 10000;

            // output mesh vars
            var _vertices = new List<Point3d>();
            var faces = new List<int>();

            // create mesh filters
            using (var filter = new AcadBRep.Mesh2dFilter())
            {
              filter.Insert(brep, control);
              using (var m = new AcadBRep.Mesh2d(filter))
              {
                foreach (var e in m.Element2ds)
                {
                  // get vertices
                  var faceIndices = new List<int>();
                  foreach (var n in e.Nodes)
                  {
                    faceIndices.Add(_vertices.Count);
                    _vertices.Add(n.Point);
                    n.Dispose();
                  }

                  // get faces
                  if (e.Nodes.Count() == 3)
                    faces.AddRange(new List<int> { 0, faceIndices[0], faceIndices[1], faceIndices[2] });
                  else if (e.Nodes.Count() == 4)
                    faces.AddRange(new List<int> { 1, faceIndices[0], faceIndices[1], faceIndices[2], faceIndices[3] });
                  e.Dispose();
                }
              }
            }
            brep.Dispose();

            // create speckle mesh
            var vertices = PointsToFlatList(_vertices);
            mesh = new Mesh(vertices, faces);
            mesh.units = ModelUnits;
            mesh.bbox = bbox;
            mesh.area = area;
            mesh.volume = volume;
          }
        }
        catch(Exception e)
        {
          Report.LogConversionError(e);
        }
      }

      return mesh;
    }

  }
}

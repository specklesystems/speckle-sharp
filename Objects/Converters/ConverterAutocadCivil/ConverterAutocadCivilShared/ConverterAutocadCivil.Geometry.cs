using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using AC = Autodesk.AutoCAD.Geometry;

using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
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
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // tolerance for geometry:
    public double tolerance = 0.000;

    // Convenience methods:
    // TODO: Deprecate once these have been added to Objects.sln
    public double[ ] PointToArray(Point3d pt)
    {
      return new double[ ] { pt.X, pt.Y, pt.Z };
    }
    public double[] PointToArray(Point2d pt)
    {
      return new double[] { pt.X, pt.Y, 0 };
    }
    public Point3d[ ] PointListToNative(IEnumerable<double> arr, string units)
    {
      var enumerable = arr.ToList();
      if (enumerable.Count % 3 != 0)throw new Speckle.Core.Logging.SpeckleException("Array malformed: length%3 != 0.");

      Point3d[ ] points = new Point3d[enumerable.Count / 3];
      var asArray = enumerable.ToArray();
      for (int i = 2, k = 0; i < enumerable.Count; i += 3)
        points[k++] = new Point3d(
          ScaleToNative(asArray[i - 2], units),
          ScaleToNative(asArray[i - 1], units),
          ScaleToNative(asArray[i], units));

      return points;
    }
    public double[ ] PointsToFlatArray(IEnumerable<Point3d> points)
    {
      return points.SelectMany(pt => PointToArray(pt)).ToArray();
    }
    public double[] PointsToFlatArray(IEnumerable<Point2d> points)
    {
      return points.SelectMany(pt => PointToArray(pt)).ToArray();
    }

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

    public List<List<ControlPoint>> ControlPointsToSpeckle(AC.NurbSurface surface, string units = null)
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
    public Interval IntervalToSpeckle(AC.Interval interval)
    {
      return new Interval(interval.LowerBound, interval.UpperBound);
    }
    public AC.Interval IntervalToNative(Interval interval)
    {
      return new AC.Interval((double)interval.start, (double)interval.end, tolerance);
    }

    // Plane 
    public Plane PlaneToSpeckle(AC.Plane plane)
    {
      Vector xAxis = VectorToSpeckle(plane.GetCoordinateSystem().Xaxis);
      Vector yAxis = VectorToSpeckle(plane.GetCoordinateSystem().Yaxis);
      var _plane = new Plane(PointToSpeckle(plane.PointOnPlane), VectorToSpeckle(plane.Normal), xAxis, yAxis, ModelUnits);
      return _plane;
    }
    public AC.Plane PlaneToNative(Plane plane)
    {
      return new AC.Plane(PointToNative(plane.origin), VectorToNative(plane.normal));
    }

    // Line
    public Line LineToSpeckle(Line2d line, string units = null)
    {
      var u = units ?? ModelUnits;

      var startParam = line.GetParameterOf(line.StartPoint);
      var endParam = line.GetParameterOf(line.EndPoint);
      var _line = new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), u);
      _line.length = line.GetLength(startParam, endParam);
      _line.domain = IntervalToSpeckle(line.GetInterval());

      return _line;
    }
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

    // Box
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
          var plane = PlaneToSpeckle(new Autodesk.AutoCAD.Geometry.Plane(origin, normal));
          box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
        }
        else
        {
          // get base plane
          var corner = new Point3d(max.X, max.Y, min.Z);
          var origin = new Point3d((corner.X + min.X) / 2, (corner.Y + min.Y) / 2, (corner.Z + min.Z) / 2);
          var plane = PlaneToSpeckle(new Autodesk.AutoCAD.Geometry.Plane(min, origin, corner));
          box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
        }

        return box;
      }
      catch
      {
        return null;
      }
    }

    // Arc
    public Arc ArcToSpeckle(CircularArc2d arc)
    {
      var interval = arc.GetInterval();
      var _arc = new Arc(PlaneToSpeckle(new AC.Plane(new Point3d(arc.Center.X, arc.Center.Y, 0), Vector3d.ZAxis)), arc.Radius, arc.StartAngle, arc.EndAngle, Math.Abs(arc.EndAngle - arc.StartAngle), ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.midPoint = PointToSpeckle(arc.EvaluatePoint((interval.UpperBound - interval.LowerBound) / 2));
      _arc.domain = IntervalToSpeckle(arc.GetInterval());
      _arc.length = arc.GetLength(arc.GetParameterOf(arc.StartPoint), arc.GetParameterOf(arc.EndPoint));
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

    // Curve
    // TODO: need to handle curves generated from polycurves with spline segments (this probably has to do with control points with varying # of knots associated with it)
    public NurbCurve3d NurbcurveToNative(Curve curve)
    {
      // process control points
      // NOTE: for **closed periodic** curves that have "n" control pts, curves sent from rhino will have n+degree points. Remove extra pts for autocad.
      var _points = PointListToNative(curve.points, curve.units).ToList();
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
      _curve.points = PointsToFlatArray(points).ToList();
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
      _curve.points = PointsToFlatArray(points).ToList();
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

    // Polycurve
    // TODO: NOT TESTED FROM HERE DOWN
    public PolylineCurve3d PolylineToNative(Polyline polyline)
    {
      var points = PointListToNative(polyline.value, polyline.units).ToList();
      if (polyline.closed)points.Add(points[0]);
      var _polyline = new PolylineCurve3d(new Point3dCollection(points.ToArray()));
      if (polyline.domain != null)
        _polyline.SetInterval(IntervalToNative(polyline.domain));
      return _polyline;
    }

    // Curve
    public Curve3d CurveToNative(ICurve icurve)
    {
      switch (icurve)
      {
        case Circle circle:
          return null;

        case Arc arc:
          return ArcToNative(arc);

        case Ellipse ellipse:
          return null;

        case Curve curve:
          return NurbcurveToNative(curve);

        case Polyline polyline:
          return PolylineToNative(polyline);

        case Line line:
          return LineToNative(line);

        case Polycurve polycurve:
          return null;

        default:
          return null;
      }
    }

    public ICurve CurveToSpeckle(Curve3d curve, string units = null)
    {
      var u = units ?? ModelUnits;

      // note: some curve3ds may not have endpoints! Not sure what contexts this may occur in, might cause issues later.
      switch (curve)
      {
        case Line3d line:
          return LineToSpeckle(line);
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
      switch (curve)
      {
        case Line2d line:
          return LineToSpeckle(line);
        case LineSegment2d line:
          return LineToSpeckle(line);
        case CircularArc2d arc:
          return ArcToSpeckle(arc);
        default:
          return NurbsToSpeckle(curve as NurbCurve2d);
      }
    }

    public Surface SurfaceToSpeckle(AC.NurbSurface surface, string units = null)
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

    public AC.NurbSurface SurfaceToNative(Geometry.Surface surface)
    {
      // Get control points
      var points = surface.GetControlPoints().Select(l => l.Select(p =>
        new ControlPoint(
          ScaleToNative(p.x, p.units),
          ScaleToNative(p.y, p.units),
          ScaleToNative(p.z, p.units),
          p.weight,
          p.units)).ToList()).ToList();

      var _surface = AC.NurbSurface.Create(new IntPtr(), true); // check what new unmanaged pointer does!!

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

  }
}

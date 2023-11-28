using System;
using System.Collections.Generic;
using System.Linq;

using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;

using Bentley.GeometryNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.DgnPlatformNET;
using BIM = Bentley.Interop.MicroStationDGN;
using BMIU = Bentley.MstnPlatformNET.InteropServices.Utilities;

using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Bentley;

public partial class ConverterBentley
{
  public double tolerance = 0.000; // tolerance for geometry

  public double[] PointToArray(DPoint2d pt)
  {
    return new double[] { pt.X, pt.Y, 0 };
  }

  public double[] PointToArray(DPoint3d pt)
  {
    return new double[] { ScaleToSpeckle(pt.X, UoR), ScaleToSpeckle(pt.Y, UoR), ScaleToSpeckle(pt.Z, UoR) };
  }

  public DPoint3d[] PointListToNative(IEnumerable<double> arr, string units)
  {
    var enumerable = arr.ToList();
    if (enumerable.Count % 3 != 0)
    {
      throw new Speckle.Core.Logging.SpeckleException("Array malformed: length%3 != 0.");
    }

    DPoint3d[] points = new DPoint3d[enumerable.Count / 3];
    var asArray = enumerable.ToArray();
    for (int i = 2, k = 0; i < enumerable.Count; i += 3)
    {
      points[k++] = new DPoint3d(
        ScaleToNative(asArray[i - 2], units, UoR),
        ScaleToNative(asArray[i - 1], units, UoR),
        ScaleToNative(asArray[i], units, UoR)
      );
    }

    return points;
  }

  public List<double> PointsToFlatList(IEnumerable<DPoint2d> points)
  {
    return points.SelectMany(pt => PointToArray(pt)).ToList();
  }

  public List<double> PointsToFlatList(IEnumerable<DPoint3d> points)
  {
    return points.SelectMany(pt => PointToArray(pt)).ToList();
  }

  // Point (2d and 3d)
  public Point Point2dToSpeckle(DPoint2d pt, string units = null)
  {
    var u = units ?? ModelUnits;
    return new Point(ScaleToSpeckle(pt.X, UoR), ScaleToSpeckle(pt.Y, UoR), 0, u);
  }

  public Point Point2dToSpeckle(Point2d pt, string units = null)
  {
    var u = units ?? ModelUnits;
    return new Point(ScaleToSpeckle(pt.X, UoR), ScaleToSpeckle(pt.Y, UoR), 0, u);
  }

  public DPoint2d Point2dToNative(Point pt)
  {
    var myPoint = new DPoint2d(ScaleToNative(pt.x, pt.units, UoR), ScaleToNative(pt.y, pt.units, UoR));

    return myPoint;
  }

  public Point Point3dToSpeckle(DPoint3d pt, string units = null)
  {
    var u = units ?? ModelUnits;
    return new Point(ScaleToSpeckle(pt.X, UoR), ScaleToSpeckle(pt.Y, UoR), ScaleToSpeckle(pt.Z, UoR), u);
  }

  public Point Point3dToSpeckle(Point3d pt, string units = null)
  {
    var u = units ?? ModelUnits;
    return new Point(ScaleToSpeckle(pt.X, UoR), ScaleToSpeckle(pt.Y, UoR), ScaleToSpeckle(pt.Z, UoR), u);
  }

  public DPoint3d Point3dToNative(Point pt)
  {
    var myPoint = new DPoint3d(
      ScaleToNative(pt.x, pt.units, UoR),
      ScaleToNative(pt.y, pt.units, UoR),
      ScaleToNative(pt.z, pt.units, UoR)
    );

    return myPoint;
  }

  public LineElement PointToNative(Point pt)
  {
    DSegment3d dSegment = new(Point3dToNative(pt), Point3dToNative(pt));
    var _line = new LineElement(Model, null, dSegment);
    return _line;
  }

  // Vector (2d and 3d)
  public Vector Vector2dToSpeckle(DVector2d pt, string units = null)
  {
    return new Vector(pt.X, pt.Y, units ?? ModelUnits);
  }

  public DVector2d Vector2dToNative(Vector pt)
  {
    return new DVector2d(ScaleToNative(pt.x, pt.units, UoR), ScaleToNative(pt.y, pt.units, UoR));
  }

  public Vector Vector3dToSpeckle(DVector3d pt, string units = null)
  {
    return new Vector(pt.X, pt.Y, pt.Z, units ?? ModelUnits);
  }

  public DVector3d VectorToNative(Vector pt)
  {
    return new DVector3d(
      ScaleToNative(pt.x, pt.units, UoR),
      ScaleToNative(pt.y, pt.units, UoR),
      ScaleToNative(pt.z, pt.units, UoR)
    );
  }

  // Interval
  public Interval IntervalToSpeckle(DRange1d range)
  {
    return new Interval(ScaleToSpeckle(range.Low, UoR), ScaleToSpeckle(range.High, UoR));
  }

  public Interval IntervalToSpeckle(DSegment1d segment)
  {
    return new Interval(ScaleToSpeckle(segment.Start, UoR), ScaleToSpeckle(segment.End, UoR));
  }

  public DRange1d IntervalToNative(Interval interval)
  {
    return DRange1d.From(
      ScaleToNative((double)interval.start, ModelUnits, UoR),
      ScaleToNative((double)interval.end, ModelUnits, UoR)
    );
  }

  public Interval2d Interval2dToSpeckle(DRange2d range)
  {
    var u = new Interval(range.Low.X, range.Low.Y);
    var v = new Interval(range.High.X, range.High.Y);
    return new Interval2d(u, v);
  }

  public DRange2d Interval2dToNative(Interval2d interval)
  {
    var u = new DPoint2d((double)interval.u.start, (double)interval.u.end);
    var v = new DPoint2d((double)interval.v.start, (double)interval.v.end);
    ;
    return DRange2d.FromPoints(u, v);
  }

  // Plane
  public Plane PlaneToSpeckle(DPlane3d plane, string units = null)
  {
    DPoint3d origin = plane.Origin;
    DVector3d normal = plane.Normal;

    DVector3d xAxis = DVector3d.UnitY.CrossProduct(plane.Normal);
    DVector3d yAxis = normal.CrossProduct(xAxis);

    var u = units ?? ModelUnits;
    var _plane = new Plane(
      Point3dToSpeckle(origin),
      Vector3dToSpeckle(normal),
      Vector3dToSpeckle(xAxis),
      Vector3dToSpeckle(yAxis),
      u
    );
    return _plane;
  }

  public Plane PlaneToSpeckle(DPoint3d pt1, DPoint3d pt2, DPoint3d pt3, string units = null)
  {
    DPoint3d origin = pt1;

    var v1 = new DVector3d(pt2.X - pt1.X, pt2.Y - pt1.Y, pt2.Z - pt1.Z);
    var v2 = new DVector3d(pt3.X - pt1.X, pt3.Y - pt1.Y, pt3.Z - pt1.Z);
    var cross = v1.CrossProduct(v2);

    cross.TryNormalize(out DVector3d normal);

    DVector3d xAxis = DVector3d.UnitY.CrossProduct(normal);
    DVector3d yAxis = normal.CrossProduct(xAxis);

    var u = units ?? ModelUnits;
    var _plane = new Plane(
      Point3dToSpeckle(origin),
      Vector3dToSpeckle(normal),
      Vector3dToSpeckle(xAxis),
      Vector3dToSpeckle(yAxis),
      u
    );
    return _plane;
  }

  public DPlane3d PlaneToNative(Plane plane)
  {
    return new DPlane3d(Point3dToNative(plane.origin), VectorToNative(plane.normal));
  }

  // Line (when the start and end point are the same, return line as point)
  public Base LineToSpeckle(LineElement line, string units = null)
  {
    CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(line);
    if (q != null)
    {
      CurveVector vec = q.GetCurveVector();
      if (vec != null)
      {
        vec.GetStartEnd(out DPoint3d startPoint, out DPoint3d endPoint);
        if (startPoint == endPoint)
        {
          return Point3dToSpeckle(startPoint, units);
        }

        double length = vec.SumOfLengths() / UoR;

        var u = units ?? ModelUnits;
        var _line = new Line(Point3dToSpeckle(startPoint), Point3dToSpeckle(endPoint), u);
        _line.length = length;
        _line.domain = new Interval(0, length);

        vec.GetRange(out var range);
        bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
        _line.bbox = BoxToSpeckle(range, worldXY);

        return _line;
      }
    }

    return new Line();
  }

  public Line LineToSpeckle(DSegment3d line, string units = null)
  {
    var u = units ?? ModelUnits;
    var _line = new Line(Point3dToSpeckle(line.StartPoint), Point3dToSpeckle(line.EndPoint), u);
    _line.length = line.Length;
    _line.domain = new Interval(0, line.Length);

    var range = DRange3d.FromPoints(line.StartPoint, line.EndPoint);
    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _line.bbox = BoxToSpeckle(range, worldXY);

    return _line;
  }

  public LineElement LineToNative(Line line)
  {
    DSegment3d dSegment = new(Point3dToNative(line.start), Point3dToNative(line.end));
    var _line = new LineElement(Model, null, dSegment);
    return _line;
  }

  // All arcs
  public ICurve ArcToSpeckle(ArcElement arc, string units = null)
  {
    double axisRatio = GetElementProperty(arc, "AxisRatio").DoubleValue;

    CurveVector vec = arc.GetCurveVector();
    vec.GetStartEnd(out DPoint3d startPoint, out DPoint3d endPoint);

    if (axisRatio == 1)
    {
      if (startPoint == endPoint)
      {
        return CircleToSpeckle(arc, units);
      }
      else
      {
        return CircularArcToSpeckle(arc, units); // Axis 1 == Axis 2
      }
    }
    else
    {
      return EllipticArcToSpeckle(arc, units); // Axis 1 != Axis 2 (return Curve instead of Arc)
    }
  }

  public ICurve ArcToSpeckle(DEllipse3d ellipse, string units = null)
  {
    ellipse.GetMajorMinorData(
      out DPoint3d center,
      out DMatrix3d matrix,
      out double majorAxis,
      out double minorAxis,
      out Angle startAngle,
      out Angle endAngle
    );
    var startPoint = ellipse.PointAtAngle(startAngle);
    var endPoint = ellipse.PointAtAngle(endAngle);
    var axisRatio = majorAxis / minorAxis;

    if (axisRatio == 1)
    {
      if (startPoint == endPoint)
      {
        return CircleToSpeckle(ellipse, units);
      }
      else
      {
        return CircularArcToSpeckle(ellipse, units); // Axis 1 == Axis 2
      }
    }
    else
    {
      return EllipticArcToSpeckle(ellipse, units); // Axis 1 != Axis 2 (return Curve instead of Arc)
    }
  }

  // Arc
  public Arc CircularArcToSpeckle(ArcElement arc, string units = null)
  {
    var u = units ?? ModelUnits;

    CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(arc);
    if (q != null)
    {
      CurveVector vec = q.GetCurveVector();
      if (vec != null)
      {
        vec.GetStartEnd(
          out DPoint3d startPoint,
          out DPoint3d endPoint,
          out DVector3d startTangent,
          out DVector3d endTangent
        );

        double length = vec.SumOfLengths();
        double radius = GetElementProperty(arc, "Radius").DoubleValue / UoR;

        // in MicroStation, positive angle is measured in CCW direction
        double startAngle = GetElementProperty(arc, "StartAngle").DoubleValue;
        double endAngle = GetElementProperty(arc, "EndAngle").DoubleValue;
        double sweep = GetElementProperty(arc, "SweepAngle").DoubleValue;
        DPoint3d center = (DPoint3d)GetElementProperty(arc, "Center").NativeValue;
        DPoint3d normal = (DPoint3d)GetElementProperty(arc, "Normal").NativeValue;
        DPlane3d plane = new(center, new DVector3d(normal));

        // an additional arc rotation can be applied in MicroStation, need to rotate the start/end points to correspond to this rotation before conversion
        double rotation = GetElementProperty(arc, "RotationAngle").DoubleValue;
        startAngle = rotation + startAngle;
        endAngle = rotation + endAngle;

        // arc rotation can also be specified about each axis
        var rotationX = GetElementProperty(arc, "RotationX");
        var rotationY = GetElementProperty(arc, "RotationY");
        var rotationZ = GetElementProperty(arc, "RotationZ");

        if (rotationX != null)
        {
          startAngle = rotationX.DoubleValue + startAngle;
          endAngle = rotationX.DoubleValue + endAngle;
        }
        else if (rotationY != null)
        {
          startAngle = -rotationY.DoubleValue + startAngle;
          endAngle = -rotationY.DoubleValue + endAngle;
        }
        else if (rotationZ != null)
        {
          startAngle = rotationZ.DoubleValue + startAngle;
          endAngle = rotationZ.DoubleValue + endAngle;
        }

        var _arc = new Arc();

        _arc.radius = radius;

        _arc.angleRadians = Math.Abs(sweep);
        _arc.startPoint = Point3dToSpeckle(startPoint);
        _arc.endPoint = Point3dToSpeckle(endPoint);
        _arc.units = u;

        if (sweep < 0)
        {
          plane.NegateNormalInPlace();
        }

        _arc.startAngle = startAngle;
        _arc.endAngle = endAngle;
        _arc.plane = PlaneToSpeckle(plane);

        CurveLocationDetail curveLoc = vec.GetPrimitive(0).PointAtSignedDistanceFromFraction(0, length / 2, false);
        var midPoint = curveLoc.Point;
        _arc.midPoint = Point3dToSpeckle(midPoint);

        _arc.length = length / UoR;
        _arc.domain = new Interval(0, length / UoR);

        vec.GetRange(out var range);
        bool worldXY = startPoint.Z == 0 && endPoint.Z == 0 ? true : false;
        _arc.bbox = BoxToSpeckle(range, worldXY);

        //if (sweep < 0)
        //{
        //  Point start = _arc.endPoint;
        //  _arc.endPoint = _arc.startPoint;
        //  _arc.startPoint = start;

        //  _arc.startAngle = endAngle;
        //  _arc.endAngle = startAngle;
        //}

        return _arc;
      }
    }

    return new Arc();
  }

  public Arc CircularArcToSpeckle(DEllipse3d ellipse, string units = null)
  {
    ellipse.IsCircular(out double radius, out DVector3d normal);

    var sweep = ellipse.SweepAngle.Radians;
    var startAngle = ellipse.StartAngle.Radians;
    var endAngle = ellipse.EndAngle.Radians;
    var center = ellipse.Center;

    var startPoint = ellipse.PointAtAngle(ellipse.StartAngle);
    var endPoint = ellipse.PointAtAngle(ellipse.EndAngle);
    var midPoint = ellipse.PointAtAngle(ellipse.StartAngle + Angle.Multiply(ellipse.SweepAngle, 0.5));

    var length = ellipse.ArcLength();

    var range = DRange3d.FromEllipse(ellipse);
    DPlane3d plane = new(center, normal);

    var _arc = new Arc();
    _arc.radius = radius / UoR;
    _arc.angleRadians = Math.Abs(sweep);
    _arc.startPoint = Point3dToSpeckle(startPoint);
    _arc.endPoint = Point3dToSpeckle(endPoint);
    _arc.midPoint = Point3dToSpeckle(midPoint);
    _arc.units = units ?? ModelUnits;

    if (sweep < 0)
    {
      plane.NegateNormalInPlace();
    }

    _arc.startAngle = startAngle;
    _arc.endAngle = endAngle;
    _arc.plane = PlaneToSpeckle(plane);

    _arc.length = length / UoR;
    _arc.domain = new Interval(0, length / UoR);

    bool worldXY = startPoint.Z == 0 && endPoint.Z == 0 ? true : false;
    _arc.bbox = BoxToSpeckle(range, worldXY);

    return _arc;
  }

  // Elliptic arc
  public Curve EllipticArcToSpeckle(ArcElement arc, string units = null)
  {
    var vec = arc.GetCurveVector();
    var primitive = vec.GetPrimitive(0); // assume one primitve in vector for single curve element

    primitive.TryGetArc(out DEllipse3d curve);

    var _spline = MSBsplineCurve.CreateFromDEllipse3d(ref curve);
    var _splineElement = new BSplineCurveElement(Model, null, _spline);

    return BSplineCurveToSpeckle(_splineElement, units);
  }

  public Curve EllipticArcToSpeckle(DEllipse3d ellipse, string units = null)
  {
    var _spline = MSBsplineCurve.CreateFromDEllipse3d(ref ellipse);
    var _splineElement = new BSplineCurveElement(Model, null, _spline);

    return BSplineCurveToSpeckle(_splineElement, units);
  }

  public ArcElement ArcToNative(Arc arc)
  {
    var radius = (double)arc.radius;
    var startAngle = (double)arc.startAngle;
    var endAngle = (double)arc.endAngle;
    var center = Point3dToNative(arc.plane.origin);

    DEllipse3d.TryCircularArcFromStartMiddleEnd(
      Point3dToNative(arc.startPoint),
      Point3dToNative(arc.midPoint),
      Point3dToNative(arc.endPoint),
      out DEllipse3d ellipse
    );

    var _arc = new ArcElement(Model, null, ellipse);
    return _arc;
  }

  // Ellipse
  public Ellipse EllipseWithoutRotationToSpeckle(EllipseElement ellipse, string units = null)
  {
    double length = ellipse.GetCurveVector().SumOfLengths() / UoR;
    double axis1 = GetElementProperty(ellipse, "PrimaryAxis").DoubleValue / UoR;
    double axis2 = GetElementProperty(ellipse, "SecondaryAxis").DoubleValue / UoR;

    var vec = ellipse.GetCurveVector();
    vec.GetRange(out DRange3d range);
    vec.CentroidNormalArea(out DPoint3d center, out DVector3d normal, out double area);

    DPlane3d plane = new(center, new DVector3d(normal));

    var u = units ?? ModelUnits;
    var _ellipse = new Ellipse(PlaneToSpeckle(plane), axis1, axis2, u);
    _ellipse.domain = new Interval(0, length);
    _ellipse.length = length;

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _ellipse.bbox = BoxToSpeckle(range, worldXY);

    _ellipse.area = area / Math.Pow(UoR, 2);

    return _ellipse;
  }

  public EllipseElement EllipseToNative(Ellipse ellipse)
  {
    var plane = PlaneToNative(ellipse.plane);

    DPlacementZX placement = new(plane.Origin);
    var e = new DEllipse3d(
      placement,
      (double)ellipse.firstRadius,
      (double)ellipse.secondRadius,
      Angle.Zero,
      Angle.TWOPI
    );
    var _ellipse = new EllipseElement(Model, null, e);

    return _ellipse;
  }

  // Ellipse element with rotation (converted to curve)
  public Curve EllipseWithRotationToSpeckle(EllipseElement ellipse, string units = null)
  {
    var vec = ellipse.GetCurveVector();
    var primitive = vec.GetPrimitive(0); // assume one primitve in vector for single curve element

    primitive.TryGetArc(out DEllipse3d curve);

    var _spline = MSBsplineCurve.CreateFromDEllipse3d(ref curve);
    var _splineElement = new BSplineCurveElement(Model, null, _spline);

    return BSplineCurveToSpeckle(_splineElement, units);
  }

  // Circle
  public Circle CircleToSpeckle(EllipseElement ellipse, string units = null)
  {
    double radius = GetElementProperty(ellipse, "Radius").DoubleValue / UoR;

    var vec = ellipse.GetCurveVector();
    vec.GetRange(out DRange3d range);
    vec.CentroidNormalArea(out DPoint3d center, out DVector3d normal, out double area);

    DPlane3d plane = new(center, new DVector3d(normal));
    var specklePlane = PlaneToSpeckle(plane);

    var u = units ?? ModelUnits;
    var _circle = new Circle(specklePlane, radius, u);
    _circle.domain = new Interval(0, 1);
    _circle.length = 2 * Math.PI * radius;
    _circle.area = Math.PI * Math.Pow(radius, 2);

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _circle.bbox = BoxToSpeckle(range, worldXY);

    return _circle;
  }

  public Circle CircleToSpeckle(ArcElement arc, string units = null)
  {
    double radius = GetElementProperty(arc, "Radius").DoubleValue / UoR;

    CurveVector vec = arc.GetCurveVector();
    vec.GetRange(out DRange3d range);
    vec.WireCentroid(out double length, out DPoint3d center);

    CurvePrimitive primitive = vec.GetPrimitive(0);
    primitive.FractionToPoint(0, out DPoint3d startPoint);
    primitive.FractionToPoint(0.25, out DPoint3d quarterPoint);

    Plane specklePlane = PlaneToSpeckle(center, quarterPoint, startPoint);

    var u = units ?? ModelUnits;
    var _circle = new Circle(specklePlane, radius, u);
    _circle.domain = new Interval(0, 1);
    _circle.length = 2 * Math.PI * radius;
    _circle.area = Math.PI * Math.Pow(radius, 2);

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _circle.bbox = BoxToSpeckle(range, worldXY);

    return _circle;
  }

  public Circle CircleToSpeckle(DEllipse3d ellipse, string units = null)
  {
    ellipse.GetMajorMinorData(
      out DPoint3d center,
      out DMatrix3d matrix,
      out double majorAxis,
      out double minorAxis,
      out Angle startAngle,
      out Angle endAngle
    );
    ellipse.IsCircular(out double radius, out DVector3d normal);
    var range = DRange3d.FromEllipse(ellipse);

    Plane specklePlane = PlaneToSpeckle(new DPlane3d(center, normal));

    var u = units ?? ModelUnits;
    var _circle = new Circle(specklePlane, radius, u);
    _circle.domain = new Interval(0, 1);
    _circle.length = 2 * Math.PI * radius;
    _circle.area = Math.PI * Math.Pow(radius, 2);

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _circle.bbox = BoxToSpeckle(range, worldXY);

    return _circle;
  }

  public EllipseElement CircleToNative(Circle ellipse)
  {
    var radius = (double)ellipse.radius;
    var plane = ellipse.plane;
    var center = Point3dToNative(plane.origin);
    var normal = VectorToNative(plane.normal);

    var e = DEllipse3d.FromCenterRadiusNormal(center, ScaleToNative(radius, ellipse.units, UoR), normal);
    var _ellipse = new EllipseElement(Model, null, e);

    return _ellipse;
  }

  // All ellipse cases (as a circle, as a curve - , as an ellipse)
  public ICurve EllipseToSpeckle(EllipseElement ellipse, string units = null)
  {
    double axisRatio = GetElementProperty(ellipse, "AxisRatio").DoubleValue;
    double rotation = GetElementProperty(ellipse, "RotationAngle").DoubleValue;

    if (axisRatio == 1)
    {
      // primary axis = secondary axis, treat as circle
      return CircleToSpeckle(ellipse, units);
    }
    else
    {
      if (rotation != 0 && rotation % (Math.PI * 2) != 0)
      {
        return EllipseWithRotationToSpeckle(ellipse, units);
      }
      else
      {
        return EllipseWithoutRotationToSpeckle(ellipse, units);
      }
    }
  }

  // Line string element
  public Polyline PolylineToSpeckle(LineStringElement lineString, string units = null)
  {
    List<DPoint3d> vertices = new();
    double totalLength = 0;
    DPoint3d firstPoint = new();
    DPoint3d lastPoint = new();
    bool closed = true;

    var segments = GetElementProperty(lineString, "Segments").ContainedValues;
    for (int i = 0; i < segments.Count(); i++)
    {
      var segment = segments.ElementAt(i);
      var segmentValues = segment.ContainedValues;
      DPoint3d startPoint = (DPoint3d)segmentValues["Start"].NativeValue;
      double length = segmentValues["Length"].DoubleValue;

      vertices.Add(startPoint);
      totalLength += length;

      if (i == 0)
      {
        firstPoint = startPoint;
      }

      if (i == segments.Count() - 1)
      {
        DPoint3d endPoint = (DPoint3d)segmentValues["End"].NativeValue;
        lastPoint = endPoint;
        if (firstPoint != lastPoint)
        {
          closed = false;
          vertices.Add(endPoint);
        }
      }
    }

    var _polyline = new Polyline(PointsToFlatList(vertices));
    _polyline.closed = closed;
    _polyline.length = totalLength / UoR;

    var curves = lineString.GetCurveVector();
    curves.GetRange(out var range);
    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _polyline.bbox = BoxToSpeckle(range, worldXY);
    _polyline.units = units ?? ModelUnits;

    return _polyline;
  }

  public Polyline PolylineToSpeckle(List<DPoint3d> pointList)
  {
    double length = 0;
    var count = pointList.Count - 1;
    for (int i = 0; i < count; i++)
    {
      var dx = pointList[i + 1].X - pointList[i].X;
      var dy = pointList[i + 1].Y - pointList[i].Y;
      var dz = pointList[i + 1].Z - pointList[i].Z;
      var d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
      length += d;
    }

    var start = pointList[0];
    var end = pointList[count];
    var closed = start.Equals(end);

    var _polyline = new Polyline(PointsToFlatList(pointList), ModelUnits);

    _polyline.closed = closed;
    _polyline.length = length / UoR;

    var range = DRange3d.FromArray(pointList);

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _polyline.bbox = BoxToSpeckle(range, worldXY);

    return _polyline;
  }

  public LineStringElement PolylineToNative(Polyline polyline)
  {
    var points = PointListToNative(polyline.value, polyline.units).ToList();
    if (polyline.closed)
    {
      points.Add(points[0]);
    }

    LineStringElement _lineString = new(Model, null, points.ToArray());
    return _lineString;
  }

  public Polycurve PolycurveToSpeckle(ComplexStringElement complexString, string units = null)
  {
    var segments = PolycurveToSpeckleList(complexString, units);

    DRange3d range = new();
    double length = 0;
    bool closed = false;
    CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(complexString);
    if (q != null)
    {
      CurveVector vec = q.GetCurveVector();
      if (vec != null)
      {
        vec.GetRange(out range);
        length = vec.SumOfLengths();
        closed = vec.IsClosedPath;
      }
    }

    var _polycurve = new Polycurve();

    _polycurve.units = units ?? ModelUnits;
    _polycurve.closed = closed;
    _polycurve.length = length;
    _polycurve.segments = segments;

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _polycurve.bbox = BoxToSpeckle(range, worldXY);

    return _polycurve;
  }

  // Complex string element (complex chain)
  public List<ICurve> PolycurveToSpeckleList(ComplexStringElement complexString, string units = null)
  {
    var segments = new List<ICurve>();

    Processor processor = new();
    ElementGraphicsOutput.Process(complexString, processor);
    var curves = processor.curveVectors;

    if (curves.Any())
    {
      foreach (var curve in curves)
      {
        foreach (var primitive in curve)
        {
          var curvePrimitiveType = primitive.GetCurvePrimitiveType();

          switch (curvePrimitiveType)
          {
            case CurvePrimitive.CurvePrimitiveType.Line:
              if (primitive.TryGetLine(out DSegment3d segment))
              {
                segments.Add(LineToSpeckle(segment));
              }
              break;
            case CurvePrimitive.CurvePrimitiveType.Arc:
              if (primitive.TryGetArc(out DEllipse3d arc))
              {
                segments.Add(ArcToSpeckle(arc));
              }
              break;
            case CurvePrimitive.CurvePrimitiveType.LineString:
              var pointList = new List<DPoint3d>();
              if (primitive.TryGetLineString(pointList))
              {
                segments.Add(PolylineToSpeckle(pointList));
              }
              break;
            case CurvePrimitive.CurvePrimitiveType.BsplineCurve:
              var spline = primitive.GetBsplineCurve();
              segments.Add(BSplineCurveToSpeckle(spline));
              break;
            case CurvePrimitive.CurvePrimitiveType.Spiral:
              var spiralSpline = primitive.GetProxyBsplineCurve();
              segments.Add(SpiralCurveElementToCurve(spiralSpline));
              break;
          }
        }
      }
    }

    return segments;
  }

  //// Complex string element (complex chain)
  public ComplexStringElement PolycurveToNative(Polycurve polycurve)
  {
    var _polycurve = new ComplexStringElement(Model, null);

    for (int i = 0; i < polycurve.segments.Count; i++)
    {
      var segment = polycurve.segments[i];
      var _curve = CurveToNative(segment);
      _polycurve.AddComponentElement(_curve);
    }

    return _polycurve;
  }

  private List<ICurve> ProcessComplexElementSegments(BIM.Element[] subElements)
  {
    var segments = new List<ICurve>();

    for (int i = 0; i < subElements.Count(); i++)
    {
      var subElementId = subElements[i].ID;
      var subElement = Model.FindElementById(new ElementId(ref subElementId));
      var subElementType = subElement.ElementType;

      switch (subElementType)
      {
        case MSElementType.Line:
          var _line = (Line)LineToSpeckle(subElement as LineElement);
          segments.Add(_line);
          break;
        case MSElementType.LineString:
          var _lineString = PolylineToSpeckle(subElement as LineStringElement);
          segments.Add(_lineString);
          break;
        case MSElementType.Arc:
          var _arc = ArcToSpeckle(subElement as ArcElement);
          segments.Add(_arc);
          break;
        case MSElementType.BsplineCurve: //lines, line strings, arcs, and curves, and open B-spline curves
          var _spline = BSplineCurveToSpeckle(subElement as BSplineCurveElement);
          segments.Add(_spline);
          break;
      }
    }

    return segments;
  }

  // Splines
  public Curve BSplineCurveToSpeckle(BSplineCurveElement curve, string units = null)
  {
    var vec = curve.GetCurveVector();
    vec.GetRange(out DRange3d range);

    var primitive = vec.GetPrimitive(0); // assume one primitve in vector for single curve element
    var curveType = primitive.GetCurvePrimitiveType();
    var _curve = new Curve();

    bool isSpiral = curveType == CurvePrimitive.CurvePrimitiveType.Spiral;
    if (isSpiral)
    {
      _curve = SpiralCurveElementToCurve(primitive);
    }
    else
    {
      MSBsplineCurve _spline = primitive.GetBsplineCurve();

      if (_spline == null)
      {
        var _proxySpline = primitive.GetProxyBsplineCurve();
        if (_proxySpline != null)
        {
          _spline = _proxySpline;
        }
        else
        {
          return null;
        }
      }

      var degree = _spline.Order - 1;
      var closed = _spline.IsClosed;
      var rational = _spline.IsRational;
      var periodic = primitive.IsPeriodicFractionSpace(out double period);
      var length = _spline.Length();
      var points = _spline.Poles;
      if (closed)
      {
        points.Add(points[0]);
      }

      var knots = (List<double>)_spline.Knots;
      var weights = (List<double>)_spline.Weights;
      if (weights == null)
      {
        weights = Enumerable.Repeat((double)1, points.Count()).ToList();
      }

      var options = new FacetOptions();
      options.SetCurveDefaultsDefaults();
      options.SetDefaults();
      options.ChordTolerance = length / 1000 / UoR;
      options.MaxEdgeLength = length / 1000 / UoR;
      var stroked = vec.Stroke(options);

      var polyPoints = new List<DPoint3d>();
      foreach (var v in stroked)
      {
        v.TryGetLineString(polyPoints);
      }

      var _points = new List<DPoint3d>();

      // get control points
      var controlPoints = GetElementProperty(curve, "ControlPointData.ControlPoints").ContainedValues;

      // get weights
      var controlPointWeights = GetElementProperty(curve, "ControlPointData.ControlPointsWeights").ContainedValues;

      // get knots
      var knotData = GetElementProperty(curve, "KnotData.Knots").ContainedValues;

      if (controlPoints.Count() > 0)
      {
        foreach (var controlPoint in controlPoints)
        {
          var point = (DPoint3d)controlPoint.NativeValue;
          _points.Add(point);
        }
      }
      else
      {
        foreach (var controlPoint in controlPointWeights)
        {
          var point = (DPoint3d)controlPoint.ContainedValues["Point"].NativeValue;
          _points.Add(point);
        }
      }

      // set nurbs curve info
      _curve.points = PointsToFlatList(_points).ToList();
      _curve.knots = knots;
      _curve.weights = weights;
      _curve.degree = degree;
      _curve.periodic = periodic;
      _curve.rational = (bool)rational;
      _curve.closed = (bool)closed;
      _curve.length = length / UoR;
      _curve.domain = new Interval(0, length / UoR);
      _curve.units = units ?? ModelUnits;

      // handle the display polyline
      try
      {
        var _polyPoints = new List<DPoint3d>();
        foreach (var pt in polyPoints)
        {
          _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));
        }

        var poly = new Polyline(PointsToFlatList(polyPoints), ModelUnits);
        _curve.displayValue = poly;
      }
      catch { }
    }

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _curve.bbox = BoxToSpeckle(range, worldXY);

    return _curve;
  }

  public Curve BSplineCurveToSpeckle(MSBsplineCurve curve, string units = null)
  {
    var degree = curve.Order - 1;
    var closed = curve.IsClosed;
    var rational = curve.IsRational;

    var range = curve.GetRange();

    //var periodic = primitive.IsPeriodicFractionSpace(out double period);
    var length = curve.Length();
    var points = curve.Poles;
    if (closed)
    {
      points.Add(points[0]);
    }

    var knots = (List<double>)curve.Knots;
    var weights = (List<double>)curve.Weights;
    if (weights == null)
    {
      weights = Enumerable.Repeat((double)1, points.Count()).ToList();
    }

    var polyPoints = new List<DPoint3d>();
    for (int i = 0; i <= 100; i++)
    {
      curve.FractionToPoint(out DPoint3d point, (double)i / 100);
      polyPoints.Add(point);
    }

    var _curve = new Curve();

    // set nurbs curve info
    _curve.points = PointsToFlatList(points).ToList();
    _curve.knots = knots;
    _curve.weights = weights;
    _curve.degree = degree;
    //_curve.periodic = periodic;
    _curve.rational = (bool)rational;
    _curve.closed = (bool)closed;
    _curve.length = length / UoR;
    _curve.domain = new Interval(0, length / UoR);
    _curve.units = units ?? ModelUnits;

    // handle the display polyline
    try
    {
      var _polyPoints = new List<DPoint3d>();
      foreach (var pt in polyPoints)
      {
        _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));
      }

      var poly = new Polyline(PointsToFlatList(polyPoints), ModelUnits);
      _curve.displayValue = poly;
    }
    catch { }

    return _curve;
  }

  public Curve SpiralCurveElementToCurve(CurvePrimitive primitive)
  {
    var _spline = primitive.GetProxyBsplineCurve();

    var degree = _spline.Order - 1;
    var closed = _spline.IsClosed;
    var rational = _spline.IsRational;
    var periodic = primitive.IsPeriodicFractionSpace(out double period);
    var length = _spline.Length();
    var points = _spline.Poles;
    if (closed)
    {
      points.Add(points[0]);
    }

    var knots = (List<double>)_spline.Knots;
    var weights = (List<double>)_spline.Weights;
    if (weights == null)
    {
      weights = Enumerable.Repeat((double)1, points.Count()).ToList();
    }

    var polyPoints = new List<DPoint3d>();
    for (int i = 0; i <= 100; i++)
    {
      _spline.FractionToPoint(out DPoint3d point, i / 100);
      polyPoints.Add(point);
    }

    var _curve = new Curve();

    // set nurbs curve info
    _curve.points = PointsToFlatList(points).ToList();
    _curve.knots = knots;
    _curve.weights = weights;
    _curve.degree = degree;
    _curve.periodic = periodic;
    _curve.rational = (bool)rational;
    _curve.closed = (bool)closed;
    _curve.length = length / UoR;
    _curve.domain = new Interval(0, length / UoR);
    _curve.units = ModelUnits;

    try
    {
      var _polyPoints = new List<DPoint3d>();
      foreach (var pt in polyPoints)
      {
        _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));
      }

      var poly = new Polyline(PointsToFlatList(polyPoints), ModelUnits);
      _curve.displayValue = poly;
    }
    catch { }

    return _curve;
  }

  public Curve SpiralCurveElementToCurve(MSBsplineCurve _spline)
  {
    var degree = _spline.Order - 1;
    var closed = _spline.IsClosed;
    var rational = _spline.IsRational;
    //var periodic = primitive.IsPeriodicFractionSpace(out double period);
    var length = _spline.Length();
    var points = _spline.Poles;
    if (closed)
    {
      points.Add(points[0]);
    }

    var knots = (List<double>)_spline.Knots;
    var weights = (List<double>)_spline.Weights;
    if (weights == null)
    {
      weights = Enumerable.Repeat((double)1, points.Count()).ToList();
    }

    var polyPoints = new List<DPoint3d>();
    for (int i = 0; i <= 100; i++)
    {
      _spline.FractionToPoint(out DPoint3d point, i / 100);
      polyPoints.Add(point);
    }

    var _curve = new Curve();

    // set nurbs curve info
    _curve.points = PointsToFlatList(points).ToList();
    _curve.knots = knots;
    _curve.weights = weights;
    _curve.degree = degree;
    //_curve.periodic = periodic;
    _curve.rational = (bool)rational;
    _curve.closed = (bool)closed;
    _curve.length = length / UoR;
    _curve.domain = new Interval(0, length / UoR);
    _curve.units = ModelUnits;

    try
    {
      var _polyPoints = new List<DPoint3d>();
      foreach (var pt in polyPoints)
      {
        _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));
      }

      var poly = new Polyline(PointsToFlatList(polyPoints), ModelUnits);
      _curve.displayValue = poly;
    }
    catch { }

    return _curve;
  }

  public BSplineCurveElement BSplineCurveToNative(Curve curve)
  {
    var points = PointListToNative(curve.points, curve.units).ToArray();
    var weights = (curve.weights.Distinct().Count() == 1) ? null : curve.weights;
    var knots = curve.knots;
    var order = curve.degree + 1;
    var closed = curve.closed;

    //var _points = PointListToNative(curve.points, curve.units).ToList();
    //if (curve.closed && curve.periodic)
    //    _points = _points.GetRange(0, _points.Count - curve.degree);
    //List<DPoint3d> points1 = _points.ToList();

    //var _knots = curve.knots;
    //if (curve.knots.Count == points.Count() + curve.degree - 1) // handles rhino format curves
    //{
    //    _knots.Insert(0, _knots[0]);
    //    _knots.Insert(_knots.Count - 1, _knots[_knots.Count - 1]);
    //}
    //if (curve.closed && curve.periodic) // handles closed periodic curves
    //    _knots = _knots.GetRange(curve.degree, _knots.Count - curve.degree * 2);
    //var knots1 = new List<double>();
    //foreach (var _knot in _knots)
    //    knots1.Add(_knot);

    //var _weights = curve.weights;
    //if (curve.closed && curve.periodic) // handles closed periodic curves
    //    _weights = curve.weights.GetRange(0, _points.Count);

    var _spline = MSBsplineCurve.CreateFromPoles(points, weights, knots, order, closed, false);

    if (curve.closed)
    {
      _spline.MakeClosed();
    }

    var _curve = new BSplineCurveElement(Model, null, _spline);
    return _curve;
  }

  // All curves
  public DisplayableElement CurveToNative(ICurve curve)
  {
    switch (curve)
    {
      case Circle circle:
        return CircleToNative(circle);

      case Arc arc:
        return ArcToNative(arc);

      case Ellipse ellipse:
        return EllipseToNative(ellipse);

      case Curve crv:
        return BSplineCurveToNative(crv);

      case Polyline polyline:
        return PolylineToNative(polyline);

      case Line line:
        return LineToNative(line);

      case Polycurve polycurve:
        return PolycurveToNative(polycurve);

      default:
        return null;
    }
  }

  public List<ICurve> TryCurveToSpeckleCurveList(DisplayableElement curve, string units = null)
  {
    var outCurves = new List<ICurve>();

    switch (curve)
    {
      case ComplexStringElement polyCurve:
        outCurves.AddRange(PolycurveToSpeckleList(polyCurve, units));
        break;

      case ArcElement arc:
        outCurves.Add(CircularArcToSpeckle(arc, units));
        break;

      case EllipseElement ellipse:
        outCurves.Add(EllipseToSpeckle(ellipse, units));
        break;

      case BSplineCurveElement crv:
        outCurves.Add(BSplineCurveToSpeckle(crv, units));
        break;

      case LineElement line:
        outCurves.Add((Line)LineToSpeckle(line, units));
        break;

      case LineStringElement polyLine:
        outCurves.Add(PolylineToSpeckle(polyLine, units));
        break;

      default:
        break;
    }

    return outCurves;
  }

  // Box
  public Box BoxToSpeckle(DRange3d range, bool OrientToWorldXY = false, string units = null)
  {
    try
    {
      Box box = null;

      var min = range.Low;
      var max = range.High;

      // get dimension intervals
      var xSize = new Interval(ScaleToSpeckle(min.X, UoR), ScaleToSpeckle(max.X, UoR));
      var ySize = new Interval(ScaleToSpeckle(min.Y, UoR), ScaleToSpeckle(max.Y, UoR));
      var zSize = new Interval(ScaleToSpeckle(min.Z, UoR), ScaleToSpeckle(max.Z, UoR));

      // get box size info
      double area = 2 * ((xSize.Length * ySize.Length) + (xSize.Length * zSize.Length) + (ySize.Length * zSize.Length));
      double volume = xSize.Length * ySize.Length * zSize.Length;

      if (OrientToWorldXY)
      {
        var origin = new DPoint3d(0, 0, 0);
        var normal = new DVector3d(0, 0, 1 * UoR);
        var plane = PlaneToSpeckle(new DPlane3d(origin, normal));
        box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
      }
      else
      {
        // get base plane
        var corner = new DPoint3d(max.X, max.Y, min.Z);
        var origin = new DPoint3d((corner.X + min.X) / 2, (corner.Y + min.Y) / 2, (corner.Z + min.Z) / 2);

        var v1 = new DVector3d(origin, corner);
        var v2 = new DVector3d(origin, min);

        var cross = v1.CrossProduct(v2);
        var plane = PlaneToSpeckle(new DPlane3d(origin, cross));
        var u = units ?? ModelUnits;
        box = new Box(plane, xSize, ySize, zSize, u) { area = area, volume = volume };
      }

      return box;
    }
    catch
    {
      return null;
    }
  }

  public DRange3d BoxToNative(Box box)
  {
    var _startPoint = new Point((double)box.xSize.start, (double)box.ySize.start, (double)box.zSize.start);
    var _endPoint = new Point((double)box.xSize.end, (double)box.ySize.end, (double)box.zSize.end);
    var startPoint = Point3dToNative(_startPoint);
    var endPoint = Point3dToNative(_endPoint);

    var _range = DRange3d.FromPoints(startPoint, endPoint);
    return _range;
  }

  // Shape
  public Polyline ShapeToSpeckle(ShapeElement shape)
  {
    var vec = shape.GetCurveVector();
    vec.CentroidNormalArea(out DPoint3d center, out DVector3d normal, out double area);
    vec.GetRange(out DRange3d range);
    var length = vec.SumOfLengths();
    var vertices = new List<DPoint3d>();
    foreach (var p in vec)
    {
      var pPoints = new List<DPoint3d>();
      p.TryGetLineString(pPoints);
      vertices.AddRange(pPoints.Distinct());
    }

    var _polyline = new Polyline(PointsToFlatList(vertices), ModelUnits) { closed = true };

    _polyline.length = length / UoR;
    _polyline.area = area / Math.Pow(UoR, 2);

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _polyline.bbox = BoxToSpeckle(range, worldXY);

    return _polyline;
  }

  // should closed polylines be converted to shapes?
  public ShapeElement ShapeToNative(Polyline shape)
  {
    var vertices = PointListToNative(shape.value, shape.units).ToArray();
    var _shape = new ShapeElement(Model, null, vertices);
    return _shape;
  }

  public Polycurve ComplexShapeToSpeckle(ComplexShapeElement shape, string units = null)
  {
    //terrible, need to figure out how to avoid using COM interface!!
    BIM.ComplexShapeElement complexShapeElement =
      BMIU.ComApp.ActiveModelReference.GetElementByID(shape.ElementId) as BIM.ComplexShapeElement;

    var closed = complexShapeElement.IsClosedElement();
    var length = complexShapeElement.Perimeter();

    var subElements = complexShapeElement.GetSubElements().BuildArrayFromContents();
    var segments = ProcessComplexElementSegments(subElements);

    DRange3d range = new();
    CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(shape);
    if (q != null)
    {
      CurveVector vec = q.GetCurveVector();
      if (vec != null)
      {
        length = vec.SumOfLengths();
        vec.GetRange(out range);
      }
    }

    var _polycurve = new Polycurve();
    _polycurve.units = units ?? ModelUnits;
    _polycurve.closed = closed;
    _polycurve.length = length;
    _polycurve.segments = segments;

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _polycurve.bbox = BoxToSpeckle(range, worldXY);

    return _polycurve;
  }

  // should closed polycurves be converted to complex shapes automatically?
  public ComplexShapeElement ComplexShapeToNative(Polycurve shape)
  {
    var _shape = new ComplexShapeElement(Model, null);

    for (int i = 0; i < shape.segments.Count; i++)
    {
      var segment = shape.segments[i];
      var _curve = CurveToNative(segment);
      _shape.AddComponentElement(_curve);
    }

    return _shape;
  }

  public Mesh MeshToSpeckle(MeshHeaderElement mesh, string units = null)
  {
    var u = units ?? ModelUnits;

    PolyfaceHeader meshData = mesh.GetMeshData();

    // get vertices
    var _vertices = meshData.Point.ToArray();

    // get faces
    var faces = new List<int>();

    var _pointIndex = meshData.PointIndex.ToList();
    var _faceIndices = new List<int>();
    for (int i = 0; i < _pointIndex.Count(); i++)
    {
      if (_pointIndex[i] != 0) // index of 0 is face loop pad/terminator
      {
        _faceIndices.Add(_pointIndex[i] - 1);
      }
      else
      {
        _faceIndices.Insert(0, _faceIndices.Count);
        faces.AddRange(_faceIndices);
        _faceIndices.Clear();
      }
    }

    // create speckle mesh
    var vertices = PointsToFlatList(_vertices);

    /*
    List<int> _colorIndex = meshData.ColorIndex.ToList();
    var defaultColour = System.Drawing.Color.FromArgb(255, 100, 100, 100);
    var colors = Enumerable.Repeat(defaultColour.ToArgb(), vertices.Count()).ToList();
    */

    var _mesh = new Mesh(vertices, faces);
    _mesh.units = u;

    meshData.ComputePrincipalAreaMoments(
      out double area,
      out DPoint3d centoid,
      out DMatrix3d axes,
      out DVector3d moments
    );
    _mesh.area = area / Math.Pow(UoR, 2);

    var range = meshData.PointRange();
    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _mesh.bbox = BoxToSpeckle(range, worldXY);

    meshData.Dispose();

    return _mesh;
  }

  public MeshHeaderElement MeshToNative(Mesh mesh)
  {
    var vertices = PointListToNative(mesh.vertices, mesh.units).ToArray();

    var meshData = new PolyfaceHeader();

    int j = 0;
    while (j < mesh.faces.Count)
    {
      int n = mesh.faces[j];
      if (n < 3)
      {
        n += 3; // 0 -> 3, 1 -> 4 to preserve backwards compatibility
      }

      List<DPoint3d> faceVertices = mesh.faces.GetRange(j + 1, n).Select(x => vertices[x]).ToList();

      if (faceVertices.Count > 0)
      {
        meshData.AddPolygon(faceVertices, new List<DVector3d>(), new List<DPoint2d>());
      }

      j += n + 1;
    }

    var _mesh = new MeshHeaderElement(Model, null, meshData);

    meshData.Dispose();

    return _mesh;
  }

  // Nurbs surface
  public Surface SurfaceToSpeckle(BSplineSurfaceElement surface, string units = null)
  {
    var u = units ?? ModelUnits;

    var nurbsSurface = surface.GetBsplineSurface();

    var knotsU = new List<double>();
    for (int i = 0; i < nurbsSurface.UKnotCount; i++)
    {
      knotsU.Add(nurbsSurface.get_UKnotAt(Convert.ToUInt32(i)));
    }

    var knotsV = new List<double>();
    for (int i = 0; i < nurbsSurface.VKnotCount; i++)
    {
      knotsV.Add(nurbsSurface.get_VKnotAt(Convert.ToUInt32(i)));
    }

    var range = nurbsSurface.GetPoleRange();
    nurbsSurface.GetParameterRegion(out double uMin, out double uMax, out double vMin, out double vMax);

    var _surface = new Surface()
    {
      degreeU = nurbsSurface.UOrder - 1,
      degreeV = nurbsSurface.VOrder - 1,
      rational = nurbsSurface.IsRational,
      closedU = nurbsSurface.IsUClosed,
      closedV = nurbsSurface.IsVClosed,
      knotsU = knotsU,
      knotsV = knotsV,
      countU = nurbsSurface.UKnotCount,
      countV = nurbsSurface.VKnotCount,
      domainU = new Interval(uMin, uMax),
      domainV = new Interval(vMin, vMax)
    };

    _surface.units = u;

    double area = GetElementProperty(surface, "SurfaceArea").DoubleValue;
    _surface.area = area / Math.Pow(UoR, 2);

    //var _points = new List<DPoint3d>();
    //for (int i = 0; i < nurbsSurface.PoleCount; i++)
    //{
    //    _points.Add(nurbsSurface.get_PoleAt(Convert.ToUInt32(i)));
    //}

    //var controlPoints = GetElementProperty(surface, "UVData.ControlPointData.ControlPoints").ContainedValues;
    //var controlPointWeights = GetElementProperty(surface, "UVData.ControlPointData.ControlPointsWeights").ContainedValues;
    //var controlPointRows = GetElementProperty(surface, "UVData.ControlPointData.ControlPointRows").ContainedValues;

    //var points = new List<List<ControlPoint>>();

    //foreach (var _row in controlPointRows)
    //{
    //    var _pts = _row.ContainedValues["ControlPoints"].ContainedValues.ToList();
    //    var _weight = _row.ContainedValues["ControlPointsWeights"].ContainedValues.ToList();

    //    var weight = new List<double>();
    //    if (!_weight.Any())
    //        weight = Enumerable.Repeat((double)1, _pts.Count()).ToList();
    //    else
    //        weight = _weight.Select(x => x.DoubleValue).ToList();
    //    for(int i = 0; i < _pts.Count(); i++)
    //    {
    //        var row = new List<ControlPoint>();
    //        var point = (DPoint3d)_pts[i].NativeValue;
    //        row.Add(new ControlPoint(ScaleToSpeckle(point.X, UoR), ScaleToSpeckle(point.Y, UoR), ScaleToSpeckle(point.Z, UoR), weight[i], null));

    //        points.Add(row);
    //    }
    //}

    var _points = ControlPointsToSpeckle(nurbsSurface);
    _surface.SetControlPoints(_points);

    bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
    _surface.bbox = BoxToSpeckle(range, worldXY);

    return _surface;
  }

  public List<List<ControlPoint>> ControlPointsToSpeckle(MSBsplineSurface surface)
  {
    var points = new List<List<ControlPoint>>();
    for (var i = 0; i < surface.PoleCount; i++)
    {
      var row = new List<ControlPoint>();

      var point = surface.get_PoleAt(Convert.ToUInt32(i));
      var weight = surface.get_WeightAt(Convert.ToUInt32(i));
      row.Add(
        new ControlPoint(
          ScaleToSpeckle(point.X, UoR),
          ScaleToSpeckle(point.Y, UoR),
          ScaleToSpeckle(point.Z, UoR),
          weight,
          ModelUnits
        )
      );

      points.Add(row);
    }
    return points;
  }

  public Base ExtendedElementToSpeckle(ExtendedElementElement extendedElement, string units = null)
  {
    var element = new Base();

    var segments = new List<ICurve>();

    Processor processor = new();
    ElementGraphicsOutput.Process(extendedElement, processor);
    var curves = processor.curveVectors;

    if (curves.Any())
    {
      foreach (var curve in curves)
      {
        foreach (var primitive in curve)
        {
          var curvePrimitiveType = primitive.GetCurvePrimitiveType();

          switch (curvePrimitiveType)
          {
            case CurvePrimitive.CurvePrimitiveType.Line:
              primitive.TryGetLine(out DSegment3d segment);
              segments.Add(LineToSpeckle(segment));
              break;
            case CurvePrimitive.CurvePrimitiveType.Arc:
              primitive.TryGetArc(out DEllipse3d arc);
              segments.Add(ArcToSpeckle(arc));
              break;
            case CurvePrimitive.CurvePrimitiveType.LineString:
              var pointList = new List<DPoint3d>();
              primitive.TryGetLineString(pointList);
              segments.Add(PolylineToSpeckle(pointList));
              break;
            case CurvePrimitive.CurvePrimitiveType.BsplineCurve:
              var spline = primitive.GetBsplineCurve();
              segments.Add(BSplineCurveToSpeckle(spline));
              break;
            case CurvePrimitive.CurvePrimitiveType.Spiral:
              var spiralSpline = primitive.GetProxyBsplineCurve();
              segments.Add(SpiralCurveElementToCurve(spiralSpline));
              break;
          }
        }
      }
    }

    element["segments"] = segments;
    return element;
  }

  public Base CellHeaderElementToSpeckle(CellHeaderElement cellHeader, string units = null)
  {
    var element = new Base();

    return element;
  }

  public CellHeaderElement CellHeaderElementToNative(Base cellHeader, string units = null)
  {
    var element = new CellHeaderElement(Model, null, new DPoint3d(), new DMatrix3d(), new List<Element>() { });

    return element;
  }

  public Base Type2ElementToSpeckle(Type2Element cellHeader, string units = null)
  {
    var element = new Base();
    Processor processor = new();
    ElementGraphicsOutput.Process(cellHeader, processor);

    var x = cellHeader.GetChildren();
    var segments = new List<ICurve>();
    var curves = processor.curveVectors;

    if (curves.Any())
    {
      foreach (var curve in curves)
      {
        foreach (var primitive in curve)
        {
          var curvePrimitiveType = primitive.GetCurvePrimitiveType();

          switch (curvePrimitiveType)
          {
            case CurvePrimitive.CurvePrimitiveType.Line:
              primitive.TryGetLine(out DSegment3d segment);
              segments.Add(LineToSpeckle(segment));
              break;
            case CurvePrimitive.CurvePrimitiveType.Arc:
              primitive.TryGetArc(out DEllipse3d arc);
              segments.Add(ArcToSpeckle(arc));
              break;
            case CurvePrimitive.CurvePrimitiveType.LineString:
              var pointList = new List<DPoint3d>();
              primitive.TryGetLineString(pointList);
              segments.Add(PolylineToSpeckle(pointList));
              break;
            case CurvePrimitive.CurvePrimitiveType.BsplineCurve:
              var spline = primitive.GetBsplineCurve();
              segments.Add(BSplineCurveToSpeckle(spline));
              break;
            case CurvePrimitive.CurvePrimitiveType.Spiral:
              var spiralSpline = primitive.GetProxyBsplineCurve();
              segments.Add(SpiralCurveElementToCurve(spiralSpline));
              break;
          }
        }
      }
    }

    element["segments"] = segments;

    return element;
  }

  internal class Processor : ElementGraphicsProcessor
  {
    private DTransform3d _transform;

    public List<CurveVector> curveVectors = new();
    public List<CurvePrimitive> curvePrimitives = new();
    public List<Base> elements = new();

    public override void AnnounceElementDisplayParameters(ElementDisplayParameters displayParams)
    {
      var asdfsaf = displayParams.ElementClass;
    }

    public override void AnnounceElementMatSymbology(ElementMatSymbology matSymb) { }

    public override void AnnounceIdentityTransform() { }

    public override void AnnounceTransform(DTransform3d trans)
    {
      _transform = trans;
    }

    public override bool ProcessAsBody(bool isCurved)
    {
      if (isCurved)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public override bool ProcessAsFacets(bool isPolyface)
    {
      if (isPolyface)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public override BentleyStatus ProcessSurface(MSBsplineSurface surface)
    {
      return BentleyStatus.Error;
    }

    public override BentleyStatus ProcessFacets(PolyfaceHeader meshData, bool filled)
    {
      return BentleyStatus.Error;
    }

    public override BentleyStatus ProcessCurveVector(CurveVector vector, bool isFilled)
    {
      vector.GetRange(out DRange3d range);
      curveVectors.Add(vector.Clone());

      return BentleyStatus.Success;
    }

    public override BentleyStatus ProcessCurvePrimitive(CurvePrimitive curvePrimitive, bool isClosed, bool isFilled)
    {
      curvePrimitives.Add(curvePrimitive);
      var curvePrimitiveType = curvePrimitive.GetCurvePrimitiveType();

      Base geometry = null;
      switch (curvePrimitiveType)
      {
        case CurvePrimitive.CurvePrimitiveType.LineString:
          var pointList = new List<DPoint3d>();
          curvePrimitive.TryGetLineString(pointList);
          //PolylineToSpeckle(pointList);

          break;
        case CurvePrimitive.CurvePrimitiveType.Arc:
          curvePrimitive.TryGetArc(out DEllipse3d arc);
          break;
        case CurvePrimitive.CurvePrimitiveType.Line:
          curvePrimitive.TryGetLine(out DSegment3d segment);
          break;
        case CurvePrimitive.CurvePrimitiveType.BsplineCurve:
          var curve = curvePrimitive.GetBsplineCurve();
          break;
      }

      return BentleyStatus.Success;
    }

    public override bool WantClipping()
    {
      return false;
    }
  }
}

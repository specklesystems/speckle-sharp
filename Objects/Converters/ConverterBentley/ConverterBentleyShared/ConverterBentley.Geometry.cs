using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.DgnPlatformNET.Elements;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using BIM = Bentley.Interop.MicroStationDGN;
using BMIU = Bentley.MstnPlatformNET.InteropServices.Utilities;
using Box = Objects.Geometry.Box;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using DisplayStyle = Objects.Other.DisplayStyle;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Bentley
{
  public partial class ConverterBentley
  {
    public static double Tolerance = 0.001;  // tolerance for geometry   

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
      if (enumerable.Count % 3 != 0) throw new Speckle.Core.Logging.SpeckleException("Array malformed: length%3 != 0.");

      DPoint3d[] points = new DPoint3d[enumerable.Count / 3];
      var asArray = enumerable.ToArray();
      for (int i = 2, k = 0; i < enumerable.Count; i += 3)
        points[k++] = new DPoint3d(
          ScaleToNative(asArray[i - 2], units, UoR),
          ScaleToNative(asArray[i - 1], units, UoR),
          ScaleToNative(asArray[i], units, UoR));

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
      var point = new DPoint2d(
        ScaleToNative(pt.x, pt.units, UoR),
        ScaleToNative(pt.y, pt.units, UoR));
      return point;
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

    public DPoint3d Point3dToNative(Point specklePoint)
    {
      var point = new DPoint3d(
        ScaleToNative(specklePoint.x, specklePoint.units, UoR),
        ScaleToNative(specklePoint.y, specklePoint.units, UoR),
        ScaleToNative(specklePoint.z, specklePoint.units, UoR));
      return point;
    }

    public LineElement PointToNative(Point specklePoint)
    {
      DSegment3d dSegment = new DSegment3d(Point3dToNative(specklePoint), Point3dToNative(specklePoint));
      var nativeElement = new LineElement(Model, null, dSegment);
      return nativeElement;
    }

    // Vector (2d and 3d)
    public Vector Vector2dToSpeckle(DVector2d nativeVector, string units = null)
    {
      return new Vector(nativeVector.X, nativeVector.Y, units ?? ModelUnits);
    }

    public DVector2d Vector2dToNative(Vector speckleVector)
    {
      return new DVector2d(
        ScaleToNative(speckleVector.x, speckleVector.units, UoR),
        ScaleToNative(speckleVector.y, speckleVector.units, UoR));
    }

    public Vector Vector3dToSpeckle(DVector3d nativeVector, string units = null)
    {
      return new Vector(nativeVector.X, nativeVector.Y, nativeVector.Z, units ?? ModelUnits);
    }

    public DVector3d VectorToNative(Vector speckleVector)
    {
      return new DVector3d(
        ScaleToNative(speckleVector.x, speckleVector.units, UoR),
        ScaleToNative(speckleVector.y, speckleVector.units, UoR),
        ScaleToNative(speckleVector.z, speckleVector.units, UoR));
    }

    // Interval
    public Interval IntervalToSpeckle(DRange1d nativeRange)
    {
      return new Interval(ScaleToSpeckle(nativeRange.Low, UoR), ScaleToSpeckle(nativeRange.High, UoR));
    }

    public Interval IntervalToSpeckle(DSegment1d nativeSegment)
    {
      return new Interval(ScaleToSpeckle(nativeSegment.Start, UoR), ScaleToSpeckle(nativeSegment.End, UoR));
    }

    public DRange1d IntervalToNative(Interval speckleInterval)
    {
      return DRange1d.From(ScaleToNative((double)speckleInterval.start, ModelUnits, UoR), ScaleToNative((double)speckleInterval.end, ModelUnits, UoR));
    }

    public Interval2d Interval2dToSpeckle(DRange2d nativeRange)
    {
      var u = new Interval(nativeRange.Low.X, nativeRange.Low.Y);
      var v = new Interval(nativeRange.High.X, nativeRange.High.Y);
      return new Interval2d(u, v);
    }

    public DRange2d Interval2dToNative(Interval2d speckleInterval2d)
    {
      var u = new DPoint2d((double)speckleInterval2d.u.start, (double)speckleInterval2d.u.end);
      var v = new DPoint2d((double)speckleInterval2d.v.start, (double)speckleInterval2d.v.end); ;
      return DRange2d.FromPoints(u, v);
    }

    // Plane 
    public Plane PlaneToSpeckle(DPlane3d nativePlane, string units = null)
    {
      DPoint3d origin = nativePlane.Origin;
      DVector3d normal = nativePlane.Normal;

      DVector3d xAxis = DVector3d.UnitY.CrossProduct(nativePlane.Normal);
      DVector3d yAxis = normal.CrossProduct(xAxis);

      var u = units ?? ModelUnits;
      var specklePlane = new Plane(Point3dToSpeckle(origin), Vector3dToSpeckle(normal), Vector3dToSpeckle(xAxis), Vector3dToSpeckle(yAxis), u);
      return specklePlane;
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
      var specklePlane = new Plane(Point3dToSpeckle(origin), Vector3dToSpeckle(normal), Vector3dToSpeckle(xAxis), Vector3dToSpeckle(yAxis), u);
      return specklePlane;
    }

    public DPlane3d PlaneToNative(Plane specklePlane)
    {
      return new DPlane3d(Point3dToNative(specklePlane.origin), VectorToNative(specklePlane.normal));
    }

    // Line (when the start and end point are the same, return line as point)
    public Base LineToSpeckle(LineElement nativeLine, string units = null)
    {
      CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(nativeLine);
      if (q != null)
      {
        CurveVector vec = q.GetCurveVector();
        if (vec != null)
        {
          vec.GetStartEnd(out DPoint3d startPoint, out DPoint3d endPoint);
          if (startPoint == endPoint)
            return Point3dToSpeckle(startPoint, units);

          double length = vec.SumOfLengths() / UoR;

          var u = units ?? ModelUnits;
          var speckleLine = new Line(Point3dToSpeckle(startPoint), Point3dToSpeckle(endPoint), u);
          speckleLine.length = length;
          speckleLine.domain = new Interval(0, length);

          vec.GetRange(out var range);
          bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
          speckleLine.bbox = BoxToSpeckle(range, worldXY);

          GetNativeProperties(nativeLine, speckleLine);

          return speckleLine;
        }
      }

      return new Line();
    }

    public Line LineToSpeckle(DSegment3d nativeLine, string units = null)
    {
      var u = units ?? ModelUnits;
      var speckleLine = new Line(Point3dToSpeckle(nativeLine.StartPoint), Point3dToSpeckle(nativeLine.EndPoint), u);
      speckleLine.length = nativeLine.Length / UoR;
      speckleLine.domain = new Interval(0, nativeLine.Length);

      var range = DRange3d.FromPoints(nativeLine.StartPoint, nativeLine.EndPoint);
      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleLine.bbox = BoxToSpeckle(range, worldXY);

      return speckleLine;
    }

    public Line LineToSpeckle(DPoint3d start, DPoint3d end, string units = null)
    {
      var u = units ?? ModelUnits;
      var speckleLine = new Line(Point3dToSpeckle(start), Point3dToSpeckle(end), u);
      double deltaX = end.X - start.X;
      double deltaY = end.Y - start.Y;
      double deltaZ = end.Z - start.Z;
      double length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
      speckleLine.length /= UoR;
      speckleLine.domain = new Interval(0, length);

      var range = DRange3d.FromPoints(start, end);
      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleLine.bbox = BoxToSpeckle(range, worldXY);

      return speckleLine;
    }

    public LineElement LineToNative(Line speckleLine)
    {
      DSegment3d dSegment = new DSegment3d(Point3dToNative(speckleLine.start), Point3dToNative(speckleLine.end));
      var nativeLine = new LineElement(Model, null, dSegment);
      return nativeLine;
    }

    // All arcs
    public ICurve ArcToSpeckle(ArcElement nativeArc, string units = null)
    {
      double axisRatio = GetElementProperty(nativeArc, "AxisRatio").DoubleValue;

      CurveVector vec = nativeArc.GetCurveVector();
      vec.GetStartEnd(out DPoint3d startPoint, out DPoint3d endPoint);

      if (axisRatio == 1)
      {
        if (startPoint == endPoint)
        {
          return CircleToSpeckle(nativeArc, units);
        }
        else
        {
          return CircularArcToSpeckle(nativeArc, units); // Axis 1 == Axis 2
        }
      }
      else
      {
        return EllipticArcToSpeckle(nativeArc, units); // Axis 1 != Axis 2 (return Curve instead of Arc)
      }
    }

    public ICurve ArcToSpeckle(DEllipse3d nativeEllipse, string units = null)
    {
      nativeEllipse.GetMajorMinorData(out DPoint3d center, out DMatrix3d matrix, out double majorAxis, out double minorAxis, out Angle startAngle, out Angle endAngle);
      var startPoint = nativeEllipse.PointAtAngle(startAngle);
      var endPoint = nativeEllipse.PointAtAngle(endAngle);
      var axisRatio = majorAxis / minorAxis;

      if (axisRatio == 1)
      {
        if (startPoint == endPoint)
        {
          return CircleToSpeckle(nativeEllipse, units);
        }
        else
        {
          return CircularArcToSpeckle(nativeEllipse, units); // Axis 1 == Axis 2
        }
      }
      else
      {
        return EllipticArcToSpeckle(nativeEllipse, units); // Axis 1 != Axis 2 (return Curve instead of Arc)
      }
    }

    // Arc
    public Arc CircularArcToSpeckle(ArcElement nativeArc, string units = null)
    {
      var u = units ?? ModelUnits;

      CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(nativeArc);
      if (q != null)
      {
        CurveVector vec = q.GetCurveVector();
        if (vec != null)
        {
          var primitive = vec.GetPrimitive(0);
          primitive.Length(out var length);

          var status = primitive.TryGetArc(out var ellipse);
          if (status)
          {
            return CircularArcToSpeckle(ellipse, u);
          }
        }
      }
      return new Arc();
    }

    public Arc CircularArcToSpeckle(DEllipse3d nativeEllipse, string units = null)
    {
      nativeEllipse.IsCircular(out double radius, out DVector3d normal);
      var length = nativeEllipse.ArcLength();
      var range = DRange3d.FromEllipse(nativeEllipse);

      var sweep = nativeEllipse.SweepAngle.Radians;
      var rotation = Angle.NormalizeRadiansToPositive(nativeEllipse.Vector0.AngleXY.Radians);

      var startAngle = nativeEllipse.StartAngle.Radians;
      var endAngle = nativeEllipse.EndAngle.Radians;

      startAngle = startAngle + rotation;
      endAngle = endAngle + rotation;

      var center = nativeEllipse.Center;

      var startPoint = nativeEllipse.PointAtAngle(nativeEllipse.StartAngle);
      var endPoint = nativeEllipse.PointAtAngle(nativeEllipse.EndAngle);

      var midPoint = nativeEllipse.PointAtAngle(nativeEllipse.StartAngle + Angle.Multiply(nativeEllipse.SweepAngle, 0.5));

      var speckleArc = new Arc();
      speckleArc.radius = radius / UoR;
      speckleArc.angleRadians = sweep;
      speckleArc.startPoint = Point3dToSpeckle(startPoint);
      speckleArc.endPoint = Point3dToSpeckle(endPoint);
      speckleArc.midPoint = Point3dToSpeckle(midPoint);

      speckleArc.startAngle = startAngle;
      speckleArc.endAngle = endAngle;

      DPlane3d plane = new DPlane3d(center, new DVector3d(normal));
      speckleArc.plane = PlaneToSpeckle(plane);

      speckleArc.length = length / UoR;
      speckleArc.domain = new Interval(0, length / UoR);

      bool worldXY = startPoint.Z == 0 && endPoint.Z == 0 ? true : false;
      speckleArc.bbox = BoxToSpeckle(range, worldXY);
      speckleArc.units = units ?? ModelUnits;

      return speckleArc;
    }

    // Elliptic arc
    public Curve EllipticArcToSpeckle(ArcElement nativeArc, string units = null)
    {
      var vec = nativeArc.GetCurveVector();
      var primitive = vec.GetPrimitive(0); // assume one primitve in vector for single curve element

      primitive.TryGetArc(out DEllipse3d curve);

      var spline = MSBsplineCurve.CreateFromDEllipse3d(ref curve);
      var splineElement = new BSplineCurveElement(Model, null, spline);

      return BSplineCurveToSpeckle(splineElement, units);
    }

    public Curve EllipticArcToSpeckle(DEllipse3d nativeEllipse, string units = null)
    {
      var spline = MSBsplineCurve.CreateFromDEllipse3d(ref nativeEllipse);
      var splineElement = new BSplineCurveElement(Model, null, spline);

      return BSplineCurveToSpeckle(splineElement, units);
    }

    public ArcElement ArcToNative(Arc speckleArc)
    {
      DEllipse3d.TryCircularArcFromStartMiddleEnd(Point3dToNative(speckleArc.startPoint), Point3dToNative(speckleArc.midPoint), Point3dToNative(speckleArc.endPoint), out DEllipse3d ellipse);

      var nativeArc = new ArcElement(Model, null, ellipse);
      return nativeArc;
    }

    // Ellipse
    public Ellipse EllipseWithoutRotationToSpeckle(EllipseElement nativeEllipse, string units = null)
    {
      double length = nativeEllipse.GetCurveVector().SumOfLengths() / UoR;
      double axis1 = GetElementProperty(nativeEllipse, "PrimaryAxis").DoubleValue / UoR;
      double axis2 = GetElementProperty(nativeEllipse, "SecondaryAxis").DoubleValue / UoR;

      var vec = nativeEllipse.GetCurveVector();
      vec.GetRange(out DRange3d range);
      vec.CentroidNormalArea(out DPoint3d center, out DVector3d normal, out double area);

      DPlane3d plane = new DPlane3d(center, new DVector3d(normal));

      var u = units ?? ModelUnits;
      var speckleEllipse = new Ellipse(PlaneToSpeckle(plane), axis1, axis2, u);
      speckleEllipse.domain = new Interval(0, length);
      speckleEllipse.length = length;

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleEllipse.bbox = BoxToSpeckle(range, worldXY);

      speckleEllipse.area = area / Math.Pow(UoR, 2);

      GetNativeProperties(nativeEllipse, speckleEllipse);

      return speckleEllipse;
    }

    public EllipseElement EllipseToNative(Ellipse speckleEllipse)
    {
      var plane = PlaneToNative((Plane)speckleEllipse.plane);

      DPlacementZX placement = new DPlacementZX(plane.Origin);
      var ellipse = new DEllipse3d(placement, (double)speckleEllipse.firstRadius, (double)speckleEllipse.secondRadius, Angle.Zero, Angle.TWOPI);
      var nativeEllipse = new EllipseElement(Model, null, ellipse);

      return nativeEllipse;
    }

    // Ellipse element with rotation (converted to curve)
    public Curve EllipseWithRotationToSpeckle(EllipseElement nativeEllipse, string units = null)
    {
      var vec = nativeEllipse.GetCurveVector();
      var primitive = vec.GetPrimitive(0); // assume one primitve in vector for single curve element
      primitive.TryGetArc(out DEllipse3d curve);

      var spline = MSBsplineCurve.CreateFromDEllipse3d(ref curve);
      var splineElement = new BSplineCurveElement(Model, null, spline);

      return BSplineCurveToSpeckle(splineElement, units);
    }

    // Circle
    public Circle CircleToSpeckle(EllipseElement nativeEllipse, string units = null)
    {
      var vec = nativeEllipse.GetCurveVector();
      vec.GetRange(out DRange3d range);
      vec.CentroidNormalArea(out DPoint3d center, out DVector3d normal, out double area);
      double radius = (vec.SumOfLengths() / (Math.PI * 2)) / UoR;

      DPlane3d plane = new DPlane3d(center, new DVector3d(normal));
      var specklePlane = PlaneToSpeckle(plane);

      var u = units ?? ModelUnits;
      var speckleCircle = new Circle(specklePlane, radius, u);
      speckleCircle.domain = new Interval(0, 1);
      speckleCircle.length = 2 * Math.PI * radius;
      speckleCircle.area = Math.PI * Math.Pow(radius, 2);

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleCircle.bbox = BoxToSpeckle(range, worldXY);

      GetNativeProperties(nativeEllipse, speckleCircle);

      return speckleCircle;
    }

    public Circle CircleToSpeckle(ArcElement nativeArc, string units = null)
    {
      CurveVector vec = nativeArc.GetCurveVector();
      vec.GetRange(out DRange3d range);
      vec.WireCentroid(out double length, out DPoint3d center);
      double radius = (vec.SumOfLengths() / (Math.PI * 2)) / UoR;

      CurvePrimitive primitive = vec.GetPrimitive(0);
      primitive.FractionToPoint(0, out DPoint3d startPoint);
      primitive.FractionToPoint(0.25, out DPoint3d quarterPoint);

      Plane specklePlane = PlaneToSpeckle(center, quarterPoint, startPoint);

      var u = units ?? ModelUnits;
      var speckleCircle = new Circle(specklePlane, radius, u);
      speckleCircle.domain = new Interval(0, 1);
      speckleCircle.length = 2 * Math.PI * radius;
      speckleCircle.area = Math.PI * Math.Pow(radius, 2);

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleCircle.bbox = BoxToSpeckle(range, worldXY);

      return speckleCircle;
    }

    public Circle CircleToSpeckle(DEllipse3d nativeEllipse, string units = null)
    {
      nativeEllipse.GetMajorMinorData(out DPoint3d center, out DMatrix3d matrix, out double majorAxis, out double minorAxis, out Angle startAngle, out Angle endAngle);
      nativeEllipse.IsCircular(out double radius, out DVector3d normal);
      var range = DRange3d.FromEllipse(nativeEllipse);

      Plane specklePlane = PlaneToSpeckle(new DPlane3d(center, normal));

      var u = units ?? ModelUnits;
      radius = ScaleToSpeckle(radius, UoR);
      var speckleCircle = new Circle(specklePlane, radius, u);
      speckleCircle.domain = new Interval(0, 1);
      speckleCircle.length = 2 * Math.PI * radius;
      speckleCircle.area = Math.PI * Math.Pow(radius, 2);

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleCircle.bbox = BoxToSpeckle(range, worldXY);

      return speckleCircle;
    }

    public EllipseElement CircleToNative(Circle speckleCircle)
    {
      var radius = (double)speckleCircle.radius;
      var plane = speckleCircle.plane;
      var center = Point3dToNative(plane.origin);
      var normal = VectorToNative(plane.normal);

      var ellipse = DEllipse3d.FromCenterRadiusNormal(center, ScaleToNative(radius, speckleCircle.units, UoR), normal);
      var nativeEllipse = new EllipseElement(Model, null, ellipse);

      return nativeEllipse;
    }

    // All ellipse cases (as a circle, as a curve - , as an ellipse)
    public ICurve EllipseToSpeckle(EllipseElement nativeEllipse, string units = null)
    {
      double axisRatio = GetElementProperty(nativeEllipse, "AxisRatio").DoubleValue;
      double rotation = GetElementProperty(nativeEllipse, "RotationAngle").DoubleValue;

      if (axisRatio == 1)
      {
        // primary axis = secondary axis, treat as circle
        return CircleToSpeckle(nativeEllipse, units);
      }
      else
      {
        if (rotation != 0 && rotation % (Math.PI * 2) != 0)
        {
          return EllipseWithRotationToSpeckle(nativeEllipse, units);
        }
        else
        {
          return EllipseWithoutRotationToSpeckle(nativeEllipse, units);
        }
      }
    }

    // Line string element
    public Polyline PolylineToSpeckle(LineStringElement nativeLineString, string units = null)
    {
      var specklePolyline = new Polyline();

      CurveVector curveVector = CurvePathQuery.ElementToCurveVector(nativeLineString);
      if (curveVector != null)
      {
        var vertices = new List<DPoint3d>();
        foreach (var primitive in curveVector)
        {
          var points = new List<DPoint3d>();
          primitive.TryGetLineString(points);
          vertices.AddRange(points);
        }
        specklePolyline.value = PointsToFlatList(vertices);

        specklePolyline.closed = curveVector.IsClosedPath;
        specklePolyline.length = curveVector.SumOfLengths() / UoR;

        curveVector.GetRange(out var range);
        bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
        specklePolyline.bbox = BoxToSpeckle(range, worldXY);
        specklePolyline.units = units ?? ModelUnits;
      }

      GetNativeProperties(nativeLineString, specklePolyline);

      return specklePolyline;
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

      var specklePolyline = new Polyline(PointsToFlatList(pointList), ModelUnits);

      specklePolyline.closed = closed;
      specklePolyline.length = length / UoR;

      var range = DRange3d.FromArray(pointList);

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      specklePolyline.bbox = BoxToSpeckle(range, worldXY);

      return specklePolyline;
    }

    public LineStringElement PolylineToNative(Polyline specklePolyline)
    {
      var points = PointListToNative(specklePolyline.value, specklePolyline.units).ToList();
      if (specklePolyline.closed)
        points.Add(points[0]);

      LineStringElement nativePolyline = new LineStringElement(Model, null, points.ToArray());
      return nativePolyline;
    }

    // Complex string element (complex chain)
    public Polycurve PolycurveToSpeckle(ComplexStringElement nativeComplexString, string units = null)
    {
      var segments = new List<ICurve>();
      CurveVector curveVector = CurvePathQuery.ElementToCurveVector(nativeComplexString);
      foreach (var primitive in curveVector)
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

      Processor processor = new Processor();
      ElementGraphicsOutput.Process(nativeComplexString, processor);

      DRange3d range = new DRange3d();
      double length = 0;
      bool closed = false;
      CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(nativeComplexString);
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

      var specklePolycurve = new Polycurve();
      specklePolycurve.units = units ?? ModelUnits;
      specklePolycurve.closed = closed;
      specklePolycurve.length = length;
      specklePolycurve.segments = segments;

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      specklePolycurve.bbox = BoxToSpeckle(range, worldXY);

      GetNativeProperties(nativeComplexString, specklePolycurve);

      return specklePolycurve;
    }

    //// Complex string element (complex chain)
    public ComplexStringElement PolycurveToNative(Polycurve specklePolycurve)
    {
      var nativePolycurve = new ComplexStringElement(Model, null);

      for (int i = 0; i < specklePolycurve.segments.Count; i++)
      {
        var segment = specklePolycurve.segments[i];
        var _curve = CurveToNative(segment);
        nativePolycurve.AddComponentElement(_curve);
      }

      return nativePolycurve;
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
    public Curve BSplineCurveToSpeckle(BSplineCurveElement nativeCurve, string units = null)
    {
      var vec = nativeCurve.GetCurveVector();
      vec.GetRange(out DRange3d range);

      var primitive = vec.GetPrimitive(0); // assume one primitve in vector for single curve element
      var curveType = primitive.GetCurvePrimitiveType();
      var speckleCurve = new Curve();

      bool isSpiral = curveType == CurvePrimitive.CurvePrimitiveType.Spiral;
      if (isSpiral)
      {
        speckleCurve = SpiralCurveElementToCurve(primitive);
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
          points.Add(points[0]);
        var knots = (List<double>)_spline.Knots;
        var weights = (List<double>)_spline.Weights;
        if (weights == null)
          weights = Enumerable.Repeat((double)1, points.Count()).ToList();

        var options = new FacetOptions();
        options.SetCurveDefaultsDefaults();
        options.SetDefaults();
        options.ChordTolerance = length / 1000 / UoR;
        options.MaxEdgeLength = length / 1000 / UoR;
        var stroked = vec.Stroke(options);

        var polyPoints = new List<DPoint3d>();
        foreach (var v in stroked)
          v.TryGetLineString(polyPoints);

        // get control points
        var controlPoints = GetElementProperty(nativeCurve, "ControlPointData.ControlPoints").ContainedValues;

        // get weights
        var controlPointWeights = GetElementProperty(nativeCurve, "ControlPointData.ControlPointsWeights").ContainedValues;

        // get knots
        var knotData = GetElementProperty(nativeCurve, "KnotData.Knots").ContainedValues;

        var _points = new List<DPoint3d>();
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
        speckleCurve.points = PointsToFlatList(_points).ToList();
        speckleCurve.knots = knots;
        speckleCurve.weights = weights;
        speckleCurve.degree = degree;
        speckleCurve.periodic = periodic;
        speckleCurve.rational = (bool)rational;
        speckleCurve.closed = (bool)closed;
        speckleCurve.length = length / UoR;
        speckleCurve.domain = new Interval(0, length / UoR);
        speckleCurve.units = units ?? ModelUnits;

        // handle the display polyline
        try
        {
          var _polyPoints = new List<DPoint3d>();
          foreach (var pt in polyPoints)
            _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));

          var poly = new Polyline(PointsToFlatList(polyPoints), ModelUnits);
          speckleCurve.displayValue = poly;
        }
        catch { }
      }

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleCurve.bbox = BoxToSpeckle(range, worldXY);

      GetNativeProperties(nativeCurve, speckleCurve);

      return speckleCurve;
    }

    public Curve BSplineCurveToSpeckle(MSBsplineCurve nativeSpline, string units = null)
    {
      var degree = nativeSpline.Order - 1;
      var closed = nativeSpline.IsClosed;
      var rational = nativeSpline.IsRational;

      var range = nativeSpline.GetRange();

      //var periodic = primitive.IsPeriodicFractionSpace(out double period);
      var length = nativeSpline.Length();
      var points = nativeSpline.Poles;
      if (closed)
        points.Add(points[0]);
      var knots = (List<double>)nativeSpline.Knots;
      var weights = (List<double>)nativeSpline.Weights;
      if (weights == null)
        weights = Enumerable.Repeat((double)1, points.Count()).ToList();

      var polyPoints = new List<DPoint3d>();
      for (int i = 0; i <= 100; i++)
      {
        nativeSpline.FractionToPoint(out DPoint3d point, (double)i / 100);
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
          _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));

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
        points.Add(points[0]);
      var knots = (List<double>)_spline.Knots;
      var weights = (List<double>)_spline.Weights;
      if (weights == null)
        weights = Enumerable.Repeat((double)1, points.Count()).ToList();

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
          _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));

        var poly = new Polyline(PointsToFlatList(polyPoints), ModelUnits);
        _curve.displayValue = poly;
      }
      catch { }

      return _curve;
    }

    public Curve SpiralCurveElementToCurve(MSBsplineCurve nativeSpline)
    {
      var degree = nativeSpline.Order - 1;
      var closed = nativeSpline.IsClosed;
      var rational = nativeSpline.IsRational;
      //var periodic = primitive.IsPeriodicFractionSpace(out double period);
      var length = nativeSpline.Length();
      var points = nativeSpline.Poles;
      if (closed)
        points.Add(points[0]);
      var knots = (List<double>)nativeSpline.Knots;
      var weights = (List<double>)nativeSpline.Weights;
      if (weights == null)
        weights = Enumerable.Repeat((double)1, points.Count()).ToList();

      var polyPoints = new List<DPoint3d>();
      for (int i = 0; i <= 100; i++)
      {
        nativeSpline.FractionToPoint(out DPoint3d point, i / 100);
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
          _polyPoints.Add(new DPoint3d(pt.X * UoR, pt.Y * UoR, pt.Z * UoR));

        var poly = new Polyline(PointsToFlatList(polyPoints), ModelUnits);
        _curve.displayValue = poly;
      }
      catch { }

      return _curve;
    }

    public BSplineCurveElement BSplineCurveToNative(Curve speckleCurve)
    {
      var points = PointListToNative(speckleCurve.points, speckleCurve.units).ToArray();
      var weights = (speckleCurve.weights.Distinct().Count() == 1) ? null : speckleCurve.weights;
      var knots = speckleCurve.knots;
      var order = speckleCurve.degree + 1;
      var closed = speckleCurve.closed;

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

      var spline = MSBsplineCurve.CreateFromPoles(points, weights, knots, order, closed, false);

      if (speckleCurve.closed)
        spline.MakeClosed();

      var nativeSpline = new BSplineCurveElement(Model, null, spline);
      return nativeSpline;
    }

    // All curves
    public DisplayableElement CurveToNative(ICurve speckleCurve)
    {
      switch (speckleCurve)
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

    public ICurve CurveToSpeckle(DisplayableElement nativeCurve, string units = null)
    {
      switch (nativeCurve)
      {
        case ComplexStringElement polyCurve:
          return PolycurveToSpeckle(polyCurve, units);

        case ArcElement arc:
          return CircularArcToSpeckle(arc, units);

        case EllipseElement ellipse:
          return EllipseToSpeckle(ellipse, units);

        case BSplineCurveElement crv:
          return BSplineCurveToSpeckle(crv, units);

        case LineElement line:
          return (Line)LineToSpeckle(line, units);

        case LineStringElement polyLine:
          return PolylineToSpeckle(polyLine, units);

        default:
          return null;
      }
    }

    // Box
    public Box BoxToSpeckle(DRange3d nativeRange, bool OrientToWorldXY = false, string units = null)
    {
      try
      {
        Box speckleBox = null;

        var min = nativeRange.Low;
        var max = nativeRange.High;

        // get dimension intervals
        Interval xSize = new Interval(ScaleToSpeckle(min.X, UoR), ScaleToSpeckle(max.X, UoR));
        Interval ySize = new Interval(ScaleToSpeckle(min.Y, UoR), ScaleToSpeckle(max.Y, UoR));
        Interval zSize = new Interval(ScaleToSpeckle(min.Z, UoR), ScaleToSpeckle(max.Z, UoR));

        // get box size info
        double area = 2 * ((xSize.Length * ySize.Length) + (xSize.Length * zSize.Length) + (ySize.Length * zSize.Length));
        double volume = xSize.Length * ySize.Length * zSize.Length;

        if (OrientToWorldXY)
        {
          var origin = new DPoint3d(0, 0, 0);
          DVector3d normal = normal = new DVector3d(0, 0, 1 * UoR);

          var plane = PlaneToSpeckle(new DPlane3d(origin, normal));
          speckleBox = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
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
          speckleBox = new Box(plane, xSize, ySize, zSize, u) { area = area, volume = volume };
        }

        return speckleBox;
      }
      catch
      {
        return null;
      }
    }

    public DRange3d BoxToNative(Box speckleBox)
    {
      var speckleStartPoint = new Point((double)speckleBox.xSize.start, (double)speckleBox.ySize.start, (double)speckleBox.zSize.start);
      var SpeckleEndPoint = new Point((double)speckleBox.xSize.end, (double)speckleBox.ySize.end, (double)speckleBox.zSize.end);
      var startPoint = Point3dToNative(speckleStartPoint);
      var endPoint = Point3dToNative(SpeckleEndPoint);

      var nativeRange = DRange3d.FromPoints(startPoint, endPoint);
      return nativeRange;
    }

    // Shape
    public Polyline ShapeToSpeckle(ShapeElement nativeShape)
    {
      var vec = nativeShape.GetCurveVector();
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

      var specklePolyline = new Polyline(PointsToFlatList(vertices), ModelUnits) { closed = true };

      specklePolyline.length = length / UoR;
      specklePolyline.area = area / Math.Pow(UoR, 2);

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      specklePolyline.bbox = BoxToSpeckle(range, worldXY);

      GetNativeProperties(nativeShape, specklePolyline);

      return specklePolyline;
    }

    // should closed polylines be converted to shapes?
    public ShapeElement ShapeToNative(Polyline specklePolyline)
    {
      var vertices = PointListToNative(specklePolyline.value, specklePolyline.units).ToArray();
      var nativeShape = new ShapeElement(Model, null, vertices);
      return nativeShape;
    }

    public Polycurve ComplexShapeToSpeckle(ComplexShapeElement nativeComplexShape, string units = null)
    {
      //terrible, need to figure out how to avoid using COM interface!! 
      BIM.ComplexShapeElement complexShapeElement = BMIU.ComApp.ActiveModelReference.GetElementByID(nativeComplexShape.ElementId) as BIM.ComplexShapeElement;

      var closed = complexShapeElement.IsClosedElement();
      var length = complexShapeElement.Perimeter();

      var subElements = complexShapeElement.GetSubElements().BuildArrayFromContents();
      var segments = ProcessComplexElementSegments(subElements);

      DRange3d range = new DRange3d();
      CurvePathQuery q = CurvePathQuery.GetAsCurvePathQuery(nativeComplexShape);
      if (q != null)
      {
        CurveVector vec = q.GetCurveVector();
        if (vec != null)
        {
          length = vec.SumOfLengths();
          vec.GetRange(out range);
        }
      }

      var specklePolycurve = new Polycurve();
      specklePolycurve.units = units ?? ModelUnits;
      specklePolycurve.closed = closed;
      specklePolycurve.length = length;
      specklePolycurve.segments = segments;

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      specklePolycurve.bbox = BoxToSpeckle(range, worldXY);

      GetNativeProperties(nativeComplexShape, specklePolycurve);

      return specklePolycurve;
    }

    // should closed polycurves be converted to complex shapes automatically?
    public ComplexShapeElement ComplexShapeToNative(Polycurve specklePolycurve)
    {
      var nativeComplexShape = new ComplexShapeElement(Model, null);

      for (int i = 0; i < specklePolycurve.segments.Count; i++)
      {
        var segment = specklePolycurve.segments[i];
        var _curve = CurveToNative(segment);
        nativeComplexShape.AddComponentElement(_curve);
      }

      return nativeComplexShape;
    }

    public Mesh MeshToSpeckle(MeshHeaderElement nativeMesh, string units = null)
    {
      PolyfaceHeader meshData = nativeMesh.GetMeshData();
      var speckleMesh = GetMeshFromPolyfaceHeader(meshData, units);

      GetNativeProperties(nativeMesh, speckleMesh);
      return speckleMesh;
    }

    public MeshHeaderElement MeshToNative(Mesh speckleMesh)
    {
      var vertices = PointListToNative(speckleMesh.vertices, speckleMesh.units).ToArray();

      var meshData = new PolyfaceHeader();

      int j = 0;
      while (j < speckleMesh.faces.Count)
      {
        int n = speckleMesh.faces[j];
        if (n < 3) n += 3; // 0 -> 3, 1 -> 4 to preserve backwards compatibility

        List<DPoint3d> faceVertices = speckleMesh.faces.GetRange(j + 1, n).Select(x => vertices[x]).ToList();

        if (faceVertices.Count > 0)
          meshData.AddPolygon(faceVertices, new List<DVector3d>(), new List<DPoint2d>());

        j += n + 1;
      }

      var nativeMesh = new MeshHeaderElement(Model, null, meshData);

      meshData.Dispose();

      return nativeMesh;
    }

    // Nurbs surface
    public Surface SurfaceToSpeckle(BSplineSurfaceElement nativeSurface, string units = null)
    {
      var u = units ?? ModelUnits;

      var nurbsSurface = nativeSurface.GetBsplineSurface();

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

      var speckleSurface = new Surface()
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

      speckleSurface.units = u;

      double area = GetElementProperty(nativeSurface, "SurfaceArea").DoubleValue;
      speckleSurface.area = area / Math.Pow(UoR, 2);

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

      var controlPoints = ControlPointsToSpeckle(nurbsSurface);
      speckleSurface.SetControlPoints(controlPoints);

      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleSurface.bbox = BoxToSpeckle(range, worldXY);

      GetNativeProperties(nativeSurface, speckleSurface);

      return speckleSurface;
    }

    public List<List<ControlPoint>> ControlPointsToSpeckle(MSBsplineSurface surface)
    {
      var points = new List<List<ControlPoint>>();
      for (var i = 0; i < surface.PoleCount; i++)
      {
        var row = new List<ControlPoint>();

        var point = surface.get_PoleAt(Convert.ToUInt32(i));
        var weight = surface.get_WeightAt(Convert.ToUInt32(i));
        row.Add(new ControlPoint(ScaleToSpeckle(point.X, UoR), ScaleToSpeckle(point.Y, UoR), ScaleToSpeckle(point.Z, UoR), weight, ModelUnits));

        points.Add(row);
      }
      return points;
    }

    public Base ExtendedElementToSpeckle(ExtendedElementElement nativeElement, string units = null)
    {
      // check for primitive solid
      var solidPrimitive = SolidPrimitiveQuery.ElementToSolidPrimitive(nativeElement);

      // check for smart solid
      Convert1.ElementToBody(out var entity, nativeElement, true, false, false);
      var solidSmartSolid = entity.EntityType == SolidKernelEntity.KernelEntityType.Solid;

      var speckleElement = new Base();
      var u = units ?? ModelUnits;

      if (solidPrimitive != null || solidSmartSolid)
      {
        MeshProcessor meshProcessor = new MeshProcessor();
        ElementGraphicsOutput.Process(nativeElement, meshProcessor);

        var mesh = meshProcessor.polyfaceHeader;
        speckleElement["@displayValue"] = GetMeshFromPolyfaceHeader(mesh, u);
      }
      else
      {
        Processor processor = new Processor();
        ElementGraphicsOutput.Process(nativeElement, processor);

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

        speckleElement["segments"] = segments;
      }

      GetNativeProperties(nativeElement, speckleElement);

      return speckleElement;
    }

    public Base CellHeaderElementToSpeckle(CellHeaderElement nativeCellHeader, string units = null)
    {
      var speckleElement = new Base();
      var u = units ?? ModelUnits;

      var cellChildren = nativeCellHeader.GetChildren();
      List<Base> children = new List<Base> { };
      foreach (var child in cellChildren)
      {
        if (CanConvertToSpeckle(child)) children.Add(ConvertToSpeckle(child));
      }

      speckleElement["@children"] = children;
      speckleElement["cellDesription"] = nativeCellHeader.CellDescription;
      speckleElement["cellName"] = nativeCellHeader.CellName;
      speckleElement["description"] = nativeCellHeader.Description;
      speckleElement["typeName"] = nativeCellHeader.TypeName;
      speckleElement["elementType"] = nativeCellHeader.ElementType;

      GetNativeProperties(nativeCellHeader, speckleElement);

      return speckleElement;
    }

    public CellHeaderElement CellHeaderElementToNative(Base speckleCellHeader, string units = null)
    {
      var element = new CellHeaderElement(Model, "", new DPoint3d(), new DMatrix3d(), new List<Element>() { });
      return element;
    }

    public Base Type2ElementToSpeckle(Type2Element nativeType2Element, string units = null)
    {
      var u = units ?? ModelUnits;

      Base speckleElement = new Base();

      Dictionary<string, object> properties = new Dictionary<string, object>();
      using (DgnECInstanceCollection instanceCollection = GetElementProperties(nativeType2Element))
      {
        foreach (IDgnECInstance instance in instanceCollection)
        {
          foreach (IECPropertyValue propertyValue in instance)
          {
            if (propertyValue != null)
            {
              properties = GetValue(properties, propertyValue);
            }
          }
        }
      };

      if (nativeType2Element is BrepCellHeaderElement) // smart solids
      {
        MeshProcessor meshProcessor = new MeshProcessor();
        ElementGraphicsOutput.Process(nativeType2Element, meshProcessor);
        var mesh = meshProcessor.polyfaceHeader;
        //element["@displayValue"] = GetMeshFromPolyfaceHeader(mesh, u);
        if (mesh != null)
          speckleElement = GetMeshFromPolyfaceHeader(mesh, u);


        var speckleBrep = new Brep(displayValue: GetMeshFromPolyfaceHeader(mesh, u), provenance: BentleyAppName, units: u);

        SolidProcessor solidProcessor = new SolidProcessor();
        ElementGraphicsOutput.Process(nativeType2Element, solidProcessor);

        var vertices = solidProcessor.vertices;
        speckleBrep.Vertices = vertices
                .Select(vertex => Point3dToSpeckle(vertex, u)).ToList();


        var faces = solidProcessor.faces;

        //// Faces
        //speckleBrep.Faces = faces
        //  .Select(f => new BrepFace(
        //    speckleBrep,
        //    faces.IndexOf(f),
        //    f.Loops.Select(l => l.LoopIndex).ToList(),
        //    f.OuterLoop.LoopIndex,
        //    f.OrientationIsReversed
        //  )).ToList();

      }
      else
      {
        Processor processor = new Processor();
        ElementGraphicsOutput.Process(nativeType2Element, processor);

        var segments = new List<ICurve>();
        var curves = processor.curveVectors;

        if (curves.Any())
        {
          foreach (var curve in curves)
          {
            curve.Transform(processor._transform);
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

        speckleElement["segments"] = segments;

#if (OPENBUILDINGS)
        string part = (string)properties["PART"];
        Category category = FindCategory(part);

        string family = (string)properties["FAMILY"];

        // levels in OBD are actually layers..
        //int level = (int)GetProperty(properties, "Level");
        //string levelName = (string)GetProperty(properties, "LEVELNAME");

        // ModifiedTime causes problems with de-serialisation atm
        properties.Remove("ModifiedTime");

        switch (category)
        {
          case (Category.Beams):
            speckleElement = BeamToSpeckle(properties, u);
            break;

          case (Category.CappingBeams):
            speckleElement = CappingBeamToSpeckle(properties, u);
            break;

          case (Category.Columns):
            speckleElement = ColumnToSpeckle(properties, u);
            break;

          case (Category.Piles):
            speckleElement = PileToSpeckle(properties, u);
            break;

          case (Category.FoundationSlabs):
          case (Category.Slabs):
            speckleElement = SlabToSpeckle(properties, segments, u);
            break;

          case (Category.Walls):
            speckleElement = WallToSpeckle(properties, segments, u);
            break;

          default:
            speckleElement = new Base();
            break;
        }
#endif
      }

      GetNativeProperties(nativeType2Element, speckleElement);

      return speckleElement;
    }

    private static Dictionary<string, object> GetValue(Dictionary<string, object> properties, IECPropertyValue propertyValue)
    {
      string propertyName = propertyValue.Property.Name;
      IECValueContainer containedValues = propertyValue.ContainedValues;
      IECValueContainer container = propertyValue.Container;
      IECProperty property = propertyValue.Property;
      IECInstance instance = propertyValue.Instance;

      string type = propertyValue.GetType().Name;

      switch (type)
      {
        case "ECDIntegerValue":
          if (propertyValue.TryGetIntValue(out int intValue))
          {
            AddProperty(properties, propertyName, intValue);
          }
          break;

        case "ECDLongValue":
        case "ECDDoubleValue":
          if (propertyValue.TryGetDoubleValue(out double doubleValue))
          {
            AddProperty(properties, propertyName, doubleValue);
          }
          break;

        case "ECDDateTimeValue":
        case "ECDStringValue":
        case "ECDCalculatedStringValue":
          if (propertyValue.TryGetStringValue(out string stringValue))
          {
            AddProperty(properties, propertyName, stringValue);
          }
          break;

        case "ECDDPoint3dValue":
        case "ECDBooleanValue":
        case "ECDArrayValue":
        case "ECDStructValue":
        case "ECDStructArrayValue":
          if (propertyValue.TryGetNativeValue(out object nativeValue))
          {
            if (nativeValue != null)
            {
              if (type == "ECDDPoint3dValue")
              {
                DPoint3d point = (DPoint3d)nativeValue;
                AddProperty(properties, propertyName, point);
                break;
              }
              else if (type == "ECDBooleanValue")
              {
                AddProperty(properties, propertyName, nativeValue);
                break;
              }
              else if (type == "ECDArrayValue")
              {
                Dictionary<string, object> arrayProperties = GetArrayValues(propertyValue);
                arrayProperties.ToList().ForEach(x => properties.Add(x.Key, x.Value));
                break;
              }
              else if (type == "ECDStructValue")
              {
                Dictionary<string, object> structProperties = GetStructValues((IECStructValue)nativeValue);
                structProperties.ToList().ForEach(x => properties.Add(x.Key, x.Value));
                break;
              }
              else if (type == "ECDStructArrayValue")
              {
                Dictionary<string, object> structArrayProperties = GetStructArrayValues((IECPropertyValue)nativeValue);
                structArrayProperties.ToList().ForEach(x => properties.Add(x.Key, x.Value));
                break;
              }
            }
          }
          break;

        default:
          break;
      }
      return properties;
    }

    // see https://communities.bentley.com/products/programming/microstation_programming/b/weblog/posts/ec-properties-related-operations-with-native-and-managed-apis
    private static Dictionary<string, object> GetArrayValues(IECPropertyValue container)
    {
      Dictionary<string, object> containedProperties = new Dictionary<string, object>();

      IECArrayValue containedValues = container.ContainedValues as IECArrayValue;
      if (containedValues != null)
      {
        for (int i = 0; i < containedValues.Count; i++)
        {
          IECPropertyValue propertyValue = containedValues[i];

          containedProperties = GetValue(containedProperties, propertyValue);
        }
      }
      return containedProperties;
    }

    private static Dictionary<string, object> GetStructValues(IECStructValue structValue)
    {
      Dictionary<string, object> containedProperties = new Dictionary<string, object>();

      foreach (IECPropertyValue containedPropertyValue in structValue)
      {
        //IECPropertyValue containedPropertyValue = enumerator.Current;
        string containedPropertyName = containedPropertyValue.Property.Name;

        containedProperties = GetValue(containedProperties, containedPropertyValue);
      }
      return containedProperties;
    }

    private static Dictionary<string, object> GetStructArrayValues(IECPropertyValue container)
    {
      Dictionary<string, object> containedProperties = new Dictionary<string, object>();

      IECStructArrayValue structArrayValue = (IECStructArrayValue)container;
      if (structArrayValue != null)
      {
        foreach (IECStructValue structValue in structArrayValue.GetStructs())
        {
          containedProperties = GetStructValues(structValue);
        }
      }
      return containedProperties;
    }

#if (OPENBUILDINGS)
    private static Category FindCategory(string part)
    {
      Category category = Category.None;
      if (part.Contains("CappingBeam"))
      {
        category = Category.CappingBeams;
      }
      else if (part.Contains("Beam"))
      {
        category = Category.Beams;
      }
      else if (part.Contains("Column"))
      {
        category = Category.Columns;
      }
      else if (part.Contains("Pile"))
      {
        category = Category.Piles;
      }
      else if (part.Contains("FoundationSlab"))
      {
        category = Category.FoundationSlabs;
      }
      else if (part.Contains("Slab"))
      {
        category = Category.Slabs;
      }
      else if (part.Contains("Wall"))
      {
        category = Category.Walls;
      }
      return category;
    }
#endif

    private static Dictionary<string, object> AddProperty(Dictionary<string, object> properties, string propertyName, object value)
    {
      if (!properties.ContainsKey(propertyName))
      {
        properties.Add(propertyName, value);
      }
      return properties;
    }

    private static Object GetProperty(Dictionary<string, object> properties, string propertyName)
    {
      if (properties.TryGetValue(propertyName, out object value))
      {
        properties.Remove(propertyName);
        return value;
      }
      return null;
    }

    public class SolidProcessor : ElementGraphicsProcessor
    {
      public DTransform3d _transform;
      public PolyfaceHeader polyfaceHeader;
      public List<DPoint3d> vertices = new List<DPoint3d>();
      public List<CurveVector> faces = new List<CurveVector>();
      public IEnumerable<SubEntity> edges;

      public override void AnnounceTransform(DTransform3d trans)
      {
        _transform = trans;
      }

      public override bool ProcessAsBody(bool isCurved)
      {
        return true;
      }

      public override bool ProcessAsFacets(bool isPolyface)
      {
        return false;
      }

      public override BentleyStatus ProcessBody(SolidKernelEntity entity)
      {
        SolidUtil.GetBodyVertices(out SubEntity[] bodyVertices, entity);
        foreach (var v in bodyVertices)
        {
          Convert1.EvaluateVertex(out DPoint3d pointOut, v);
          vertices.Add(pointOut);
        }


        SolidUtil.GetBodyFaces(out SubEntity[] bodyFaces, entity);
        foreach (var f in bodyFaces)
        {
          SolidUtil.GetFaceParameterRange(out var uRange, out var vRange, f);

          DPoint2d uvParam = new DPoint2d();
          uvParam.X = (uRange.Low + uRange.High) * 0.5;
          uvParam.Y = (vRange.Low + vRange.High) * 0.5;
          Convert1.EvaluateFace(out var pointOut, out var normalOut, out var uDirOut, out var vDirOut, f, uvParam);
          Convert1.SubEntityToCurveVector(out var curvesOut, f);
          faces.Add(curvesOut);
        }

        SolidUtil.GetBodyEdges(out SubEntity[] bodyEdges, entity);
        edges = bodyEdges;



        return BentleyStatus.Success;
      }

      public override BentleyStatus ProcessFacets(PolyfaceHeader meshData, bool filled)
      {
        var polyfaceHeaderData = new PolyfaceHeader();
        polyfaceHeaderData.CopyFrom(meshData);
        polyfaceHeader = polyfaceHeaderData;
        return BentleyStatus.Success;
      }

      public override bool WantClipping()
      {
        return false;
      }
    }


    public class MeshProcessor : ElementGraphicsProcessor
    {
      public DTransform3d _transform;
      public PolyfaceHeader polyfaceHeader;

      public override void AnnounceTransform(DTransform3d trans)
      {
        _transform = trans;
      }

      public override bool ProcessAsBody(bool isCurved)
      {
        return false;
      }

      public override bool ProcessAsFacets(bool isPolyface)
      {
        return true;
      }

      public override BentleyStatus ProcessFacets(PolyfaceHeader meshData, bool filled)
      {
        var polyfaceHeaderData = new PolyfaceHeader();
        polyfaceHeaderData.CopyFrom(meshData);
        polyfaceHeader = polyfaceHeaderData;
        return BentleyStatus.Success;
      }

      public override bool WantClipping()
      {
        return false;
      }
    }

    public class Processor : ElementGraphicsProcessor
    {
      public DTransform3d _transform;

      public List<MSBsplineSurface> surfaces = new List<MSBsplineSurface>();
      public PolyfaceHeader polyfaceHeader = new PolyfaceHeader();
      public List<CurveVector> curveVectors = new List<CurveVector>();
      public List<CurvePrimitive> curvePrimitives = new List<CurvePrimitive>();
      public List<Base> elements = new List<Base>();
      public DisplayStyle displayStyle;

      public override void AnnounceElementDisplayParameters(ElementDisplayParameters displayParams)
      {
        var style = new DisplayStyle();
        var color = displayParams.IsLineColorTBGR ? displayParams.LineColor : displayParams.LineColorTBGR;
        style.color = (int)color;

        var lineType = displayParams.LineStyle;
        displayStyle = style;

        var c = Color.FromArgb(style.color);
        var level = displayParams.Level;
      }

      public override bool ExpandLineStyles()
      {
        return true;
      }

      public override void AnnounceElementMatSymbology(ElementMatSymbology matSymb)
      {
      }

      public override void AnnounceIdentityTransform()
      {
      }

      public override void AnnounceTransform(DTransform3d trans)
      {
        _transform = trans;
      }

      public override bool ProcessAsBody(bool isCurved)
      {
        // needs to return false, so columns get processed as primitive geometry
        //if (isCurved)
        //  return true;
        //else
        return false;
      }

      public override bool ProcessAsFacets(bool isPolyface)
      {
        if (isPolyface)
          return true;
        else
          return false;
      }

      public override BentleyStatus ProcessBody(SolidKernelEntity entity)
      {
        return BentleyStatus.Error;
      }

      public override BentleyStatus ProcessSolidPrimitive(SolidPrimitive primitive)
      {
        var y = primitive.TryGetDgnExtrusionDetail();
        return BentleyStatus.Error;
      }

      public override BentleyStatus ProcessSurface(MSBsplineSurface surface)
      {

        return BentleyStatus.Success;
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

    private static List<Element> GetChildren(Element parent)
    {
      List<Element> children = new List<Element>();
      IEnumerator<Element> enumerator = parent.GetChildren().GetEnumerator();
      while (enumerator.MoveNext())
      {
        Element child = enumerator.Current;
        children.Add(child);
        children.AddRange(GetChildren(child));
      }
      return children;
    }

    public Element BrepToNative(Brep speckleBrep, string units = null)
    {
      //create solid from mesh
      var displayMesh = speckleBrep.displayValue;
      var mesh = displayMesh.Select(m => MeshToNative(m));
      Convert1.ElementToBody(out var entity, mesh.First(), true, true, true);
      Convert1.BodyToElement(out var nativeElement, entity, null, Model);

      return nativeElement;
    }

    public Brep SolidElementToSpeckle(SolidElement nativeSolid, string units = null)
    {
      var solidPrim = nativeSolid.GetSolidPrimitive();
      Convert1.ElementToBody(out var entity, nativeSolid, true, true, true);
      SolidUtil.GetBodyFaces(out var subEntities, entity);

      foreach (var e in subEntities)
      {
        var subType = e.EntitySubType;
        Convert1.SubEntityToCurveVector(out var curves, e);
      }

      var u = units ?? ModelUnits;
      var speckleBrep = new Brep();
      speckleBrep.units = u;

      speckleBrep["description"] = nativeSolid.Description;
      speckleBrep["typeName"] = nativeSolid.TypeName;
      speckleBrep["elementType"] = nativeSolid.ElementType;

      if (nativeSolid is null) return null;

      var faceIndex = 0;
      var edgeIndex = 0;
      var curve2dIndex = 0;
      var curve3dIndex = 0;
      var loopIndex = 0;
      var trimIndex = 0;
      var surfaceIndex = 0;

      speckleBrep.displayValue = new List<Mesh> { GetMeshFromSolid(nativeSolid, u) };


      SolidProcessor processor = new SolidProcessor();
      ElementGraphicsOutput.Process(nativeSolid, processor);




      //var speckleFaces = new Dictionary<Face, BrepFace>();
      //var speckleEdges = new Dictionary<Edge, BrepEdge>();
      //var speckleEdgeIndexes = new Dictionary<Edge, int>();
      //var speckle3dCurves = new ICurve[solid.Edges.Size];
      //var speckle2dCurves = new List<ICurve>();
      //var speckleLoops = new List<BrepLoop>();
      //var speckleTrims = new List<BrepTrim>();

      //foreach (var face in solid.Faces.Cast<Face>())
      //{
      //  var surface = FaceToSpeckle(face, out bool orientation, 0.0);
      //  var iterator = face.EdgeLoops.ForwardIterator();
      //  var loopIndices = new List<int>();

      //  while (iterator.MoveNext())
      //  {
      //    var loop = iterator.Current as EdgeArray;
      //    var loopTrimIndices = new List<int>();
      //    // Loop through the edges in the loop.
      //    var loopIterator = loop.ForwardIterator();
      //    while (loopIterator.MoveNext())
      //    {
      //      // Each edge should create a 2d curve, a 3d curve, a BrepTrim and a BrepEdge.
      //      var edge = loopIterator.Current as Edge;
      //      var faceA = edge.GetFace(0);

      //      // Determine what face side are we currently on.
      //      var edgeSide = face == faceA ? 0 : 1;

      //      // Get curve, create trim and save index
      //      var trim = edge.GetCurveUV(edgeSide);
      //      var sTrim = new BrepTrim(brep, edgeIndex, faceIndex, loopIndex, curve2dIndex, 0, BrepTrimType.Boundary, edge.IsFlippedOnFace(edgeSide), -1, -1);
      //      var sTrimIndex = trimIndex;
      //      loopTrimIndices.Add(sTrimIndex);

      //      // Add curve and trim, increase index counters.
      //      speckle2dCurves.Add(CurveToSpeckle(trim.As3DCurveInXYPlane(), Units.None));
      //      speckleTrims.Add(sTrim);
      //      curve2dIndex++;
      //      trimIndex++;

      //      // Check if we have visited this edge before.
      //      if (!speckleEdges.ContainsKey(edge))
      //      {
      //        // First time we visit this edge, add 3d curve and create new BrepEdge.
      //        var edgeCurve = edge.AsCurve();
      //        speckle3dCurves[curve3dIndex] = CurveToSpeckle(edgeCurve, u);
      //        var sCurveIndex = curve3dIndex;
      //        curve3dIndex++;

      //        // Create a trim with just one of the trimIndices set, the second one will be set on the opposite condition.
      //        var sEdge = new BrepEdge(brep, sCurveIndex, new[] { sTrimIndex }, -1, -1, edge.IsFlippedOnFace(face), null);
      //        speckleEdges.Add(edge, sEdge);
      //        speckleEdgeIndexes.Add(edge, edgeIndex);
      //        edgeIndex++;
      //      }
      //      else
      //      {
      //        // Already visited this edge, skip curve 3d
      //        var sEdge = speckleEdges[edge];
      //        var sEdgeIndex = speckleEdgeIndexes[edge];
      //        sTrim.EdgeIndex = sEdgeIndex;

      //        // Update trim indices with new item.
      //        // TODO: Make this better.
      //        var trimIndices = sEdge.TrimIndices.ToList();
      //        trimIndices.Append(sTrimIndex); //TODO Append is a pure function and the return is unused
      //        sEdge.TrimIndices = trimIndices.ToArray();
      //      }
      //    }

      //    var speckleLoop = new BrepLoop(brep, faceIndex, loopTrimIndices, BrepLoopType.Outer);
      //    speckleLoops.Add(speckleLoop);
      //    var sLoopIndex = loopIndex;
      //    loopIndex++;
      //    loopIndices.Add(sLoopIndex);
      //  }

      //  speckleFaces.Add(face,
      //    new BrepFace(brep, surfaceIndex, loopIndices, loopIndices[0], !face.OrientationMatchesSurfaceOrientation));
      //  faceIndex++;
      //  brep.Surfaces.Add(surface);
      //  surfaceIndex++;
      //}

      //brep.Faces = speckleFaces.Values.ToList();
      //brep.Curve2D = speckle2dCurves;
      //brep.Curve3D = speckle3dCurves.ToList();
      //brep.Trims = speckleTrims;
      //brep.Edges = speckleEdges.Values.ToList();
      //brep.Loops = speckleLoops;
      //brep.displayValue = GetMeshesFromSolids(new[] { solid });

      GetNativeProperties(nativeSolid, speckleBrep);

      return speckleBrep;
    }


    public Brep ConeElementToSpeckle(ConeElement nativeSolid, string units = null)
    {
      var u = units ?? ModelUnits;
      var mesh = GetMeshFromSolid(nativeSolid, u);
      var speckleBrep = new Brep(displayValue: mesh, provenance: BentleyAppName, units: u);

      var solidPrim = nativeSolid.GetSolidPrimitive();
      Convert1.ElementToBody(out var entity, nativeSolid, true, true, true);
      SolidUtil.GetBodyFaces(out var subEntities, entity);

      foreach (var e in subEntities)
      {
        var subType = e.EntitySubType;
        Convert1.SubEntityToCurveVector(out var curves, e);
      }



      speckleBrep["description"] = nativeSolid.Description;
      speckleBrep["typeName"] = nativeSolid.TypeName;
      speckleBrep["elementType"] = nativeSolid.ElementType;


      SolidProcessor processor = new SolidProcessor();
      ElementGraphicsOutput.Process(nativeSolid, processor);

      var vertices = processor.vertices;
      //var converted = new List<CurveVector> { };
      //foreach (var v in vertices)
      //{
      //  Convert1.SubEntityToCurveVector(out var curves, v);
      //  converted.Add(curves);
      //}


      // Vertices, uv curves, 3d curves and surfaces
      //speckleBrep.Vertices = Vertices
      //  .Select(vertex => PointToSpeckle(vertex, u)).ToList();

      if (nativeSolid is null) return null;

      var faceIndex = 0;
      var edgeIndex = 0;
      var curve2dIndex = 0;
      var curve3dIndex = 0;
      var loopIndex = 0;
      var trimIndex = 0;
      var surfaceIndex = 0;



      speckleBrep.displayValue = new List<Mesh> { mesh };

      //var speckleFaces = new Dictionary<Face, BrepFace>();
      //var speckleEdges = new Dictionary<Edge, BrepEdge>();
      //var speckleEdgeIndexes = new Dictionary<Edge, int>();
      //var speckle3dCurves = new ICurve[solid.Edges.Size];
      //var speckle2dCurves = new List<ICurve>();
      //var speckleLoops = new List<BrepLoop>();
      //var speckleTrims = new List<BrepTrim>();

      //foreach (var face in solid.Faces.Cast<Face>())
      //{
      //  var surface = FaceToSpeckle(face, out bool orientation, 0.0);
      //  var iterator = face.EdgeLoops.ForwardIterator();
      //  var loopIndices = new List<int>();

      //  while (iterator.MoveNext())
      //  {
      //    var loop = iterator.Current as EdgeArray;
      //    var loopTrimIndices = new List<int>();
      //    // Loop through the edges in the loop.
      //    var loopIterator = loop.ForwardIterator();
      //    while (loopIterator.MoveNext())
      //    {
      //      // Each edge should create a 2d curve, a 3d curve, a BrepTrim and a BrepEdge.
      //      var edge = loopIterator.Current as Edge;
      //      var faceA = edge.GetFace(0);

      //      // Determine what face side are we currently on.
      //      var edgeSide = face == faceA ? 0 : 1;

      //      // Get curve, create trim and save index
      //      var trim = edge.GetCurveUV(edgeSide);
      //      var sTrim = new BrepTrim(brep, edgeIndex, faceIndex, loopIndex, curve2dIndex, 0, BrepTrimType.Boundary, edge.IsFlippedOnFace(edgeSide), -1, -1);
      //      var sTrimIndex = trimIndex;
      //      loopTrimIndices.Add(sTrimIndex);

      //      // Add curve and trim, increase index counters.
      //      speckle2dCurves.Add(CurveToSpeckle(trim.As3DCurveInXYPlane(), Units.None));
      //      speckleTrims.Add(sTrim);
      //      curve2dIndex++;
      //      trimIndex++;

      //      // Check if we have visited this edge before.
      //      if (!speckleEdges.ContainsKey(edge))
      //      {
      //        // First time we visit this edge, add 3d curve and create new BrepEdge.
      //        var edgeCurve = edge.AsCurve();
      //        speckle3dCurves[curve3dIndex] = CurveToSpeckle(edgeCurve, u);
      //        var sCurveIndex = curve3dIndex;
      //        curve3dIndex++;

      //        // Create a trim with just one of the trimIndices set, the second one will be set on the opposite condition.
      //        var sEdge = new BrepEdge(brep, sCurveIndex, new[] { sTrimIndex }, -1, -1, edge.IsFlippedOnFace(face), null);
      //        speckleEdges.Add(edge, sEdge);
      //        speckleEdgeIndexes.Add(edge, edgeIndex);
      //        edgeIndex++;
      //      }
      //      else
      //      {
      //        // Already visited this edge, skip curve 3d
      //        var sEdge = speckleEdges[edge];
      //        var sEdgeIndex = speckleEdgeIndexes[edge];
      //        sTrim.EdgeIndex = sEdgeIndex;

      //        // Update trim indices with new item.
      //        // TODO: Make this better.
      //        var trimIndices = sEdge.TrimIndices.ToList();
      //        trimIndices.Append(sTrimIndex); //TODO Append is a pure function and the return is unused
      //        sEdge.TrimIndices = trimIndices.ToArray();
      //      }
      //    }

      //    var speckleLoop = new BrepLoop(brep, faceIndex, loopTrimIndices, BrepLoopType.Outer);
      //    speckleLoops.Add(speckleLoop);
      //    var sLoopIndex = loopIndex;
      //    loopIndex++;
      //    loopIndices.Add(sLoopIndex);
      //  }

      //  speckleFaces.Add(face,
      //    new BrepFace(brep, surfaceIndex, loopIndices, loopIndices[0], !face.OrientationMatchesSurfaceOrientation));
      //  faceIndex++;
      //  brep.Surfaces.Add(surface);
      //  surfaceIndex++;
      //}

      //brep.Faces = speckleFaces.Values.ToList();
      //brep.Curve2D = speckle2dCurves;
      //brep.Curve3D = speckle3dCurves.ToList();
      //brep.Trims = speckleTrims;
      //brep.Edges = speckleEdges.Values.ToList();
      //brep.Loops = speckleLoops;
      //brep.displayValue = GetMeshesFromSolids(new[] { solid });

      GetNativeProperties(nativeSolid, speckleBrep);

      return speckleBrep;
    }


    public Mesh GetMeshFromPolyfaceHeader(PolyfaceHeader meshData, string units = null)
    {
      //meshData.Triangulate();

      // get vertices
      var vertices = meshData.Point.ToArray();

      // get faces
      var faces = new List<int>();

      var faceIndices = new List<int>();
      var pointIndex = meshData.PointIndex.ToList();


      // check NumberPerFace property
      if(meshData.NumberPerFace > 1)
      {
        var faceCount = 0;
        for (int i = 0; i < pointIndex.Count(); i++)
        {
          if (faceCount < meshData.NumberPerFace && pointIndex[i] != 0)
          {
            faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
            faceCount++;
          }
          else
          {
            switch (faceIndices.Count())
            {
              //case 3:
              //  faceIndices.Insert(0, 0);
              //  break;
              //case 4:
              //  faceIndices.Insert(0, 1);
              //  break;
              default:
                faceIndices.Insert(0, faceIndices.Count());
                break;
            }
            faces.AddRange(faceIndices);
            faceIndices.Clear();
            faceCount = 0;

            if (i < pointIndex.Count() && pointIndex[i] != 0)
            {
              faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
              faceCount = 1;
            }
          }
        }
      } else
      {
        for (int i = 0; i < pointIndex.Count(); i++)
        {
          if (pointIndex[i] != 0 && (pointIndex[i] != pointIndex[i + 1])) // index of 0 is face loop pad/terminator
            faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
          else
          {
            if (i < pointIndex.Count() - 1)
            {
              if (pointIndex[i] == pointIndex[i + 1])
              {
                faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
              }
            }
            else
            {

            }

            switch (faceIndices.Count())
            {
              //case 3:
              //  faceIndices.Insert(0, 0);
              //  break;
              //case 4:
              //  faceIndices.Insert(0, 1);
              //  break;
              default:
                faceIndices.Insert(0, faceIndices.Count());
                break;
            }
            faces.AddRange(faceIndices);
            faceIndices.Clear();
          }
        }
      }

      // face loop terminator is 0 for planar mesh
      // for non planar mesh, use number of points per face as terminator
      //if (pointIndex.Contains(0))
      //{
      //  for (int i = 0; i < pointIndex.Count(); i++)
      //  {
      //    if (pointIndex[i] != 0 && (pointIndex[i] != pointIndex[i + 1])) // index of 0 is face loop pad/terminator
      //      faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
      //    else
      //    {
      //      if (i < pointIndex.Count() -1)
      //      {
      //        if (pointIndex[i] == pointIndex[i + 1])
      //        {
      //          faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
      //        }
      //      }
      //      else { 
            
      //      }

      //      switch (faceIndices.Count())
      //      {
      //        //case 3:
      //        //  faceIndices.Insert(0, 0);
      //        //  break;
      //        //case 4:
      //        //  faceIndices.Insert(0, 1);
      //        //  break;
      //        default:
      //          faceIndices.Insert(0, faceIndices.Count());
      //          break;
      //      }
      //      faces.AddRange(faceIndices);
      //      faceIndices.Clear();
      //    }
      //  }
      //}
      //else
      //{
      //  var faceCount = 0;
      //  for (int i = 0; i <= pointIndex.Count(); i++)
      //  {
      //    if (faceCount < meshData.NumberPerFace)
      //    {
      //      faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
      //      faceCount++;
      //    }
      //    else
      //    {
      //      switch (faceIndices.Count())
      //      {
      //        case 3:
      //          faceIndices.Insert(0, 0);
      //          break;
      //        case 4:
      //          faceIndices.Insert(0, 1);
      //          break;
      //        default:
      //          faceIndices.Insert(0, faceIndices.Count());
      //          break;
      //      }
      //      faces.AddRange(faceIndices);
      //      faceIndices.Clear();

      //      if (i < pointIndex.Count())
      //      {
      //        faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
      //        faceCount = 1;
      //      }
      //    }
      //  }
      //}

      //if (meshData.NumberPerFace > 1)
      //{
      //  var faceCounter = 0;
      //  for (int i = 0; i <= pointIndex.Count(); i++)
      //  {
      //    if (faceCounter < meshData.NumberPerFace) {
      //      faceIndices.Add(Math.Abs(pointIndex[i]));
      //      faceCounter++;
      //    } else
      //    {
      //      faceIndices.Insert(0, (int)meshData.NumberPerFace);
      //      faces.AddRange(faceIndices);
      //      faceIndices.Clear();
      //      faceCounter = 0;
      //    }
      //  }
      //}
      //else
      //{
      //  for (int i = 0; i < pointIndex.Count(); i++)
      //  {
      //    if (pointIndex[i] != 0) // index of 0 is face loop pad/terminator for planar mesh
      //      faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
      //    else
      //    {
      //      switch (faceIndices.Count())
      //      {
      //        case 3:
      //          faceIndices.Insert(0, 0);
      //          break;
      //        case 4:
      //          faceIndices.Insert(0, 1);
      //          break;
      //        default:
      //          faceIndices.Insert(0, faceIndices.Count());
      //          break;
      //      }
      //      faces.AddRange(faceIndices);
      //      faceIndices.Clear();
      //    }
      //  }
      //}

      //if (pointIndex[i] != 0) // index of 0 is face loop pad/terminator
      //  faceIndices.Add(Math.Abs(pointIndex[i]) - 1);
      //else
      //{
      //  switch (faceIndices.Count())
      //  {
      //    case 3:
      //      faceIndices.Insert(0, 0);
      //      break;
      //    case 4:
      //      faceIndices.Insert(0, 1);
      //      break;
      //    default:
      //      faceIndices.Insert(0, faceIndices.Count());
      //      break;
      //  }
      //  faces.AddRange(faceIndices);
      //  faceIndices.Clear();
      //}


      // create speckle mesh
      var speckleMesh = new Mesh(PointsToFlatList(vertices), faces);
      var u = units ?? ModelUnits;
      speckleMesh.units = u;

      meshData.ComputePrincipalAreaMoments(out double area, out DPoint3d centoid, out DMatrix3d axes, out DVector3d moments);
      speckleMesh.area = area / Math.Pow(UoR, 2);

      meshData.ComputePrincipalMomentsAllowMissingSideFacets(out double volume, out var centroid, out var axes1, out var moments1, false, 1.0e-8);
      speckleMesh.volume = volume / Math.Pow(UoR, 3);

      var range = meshData.PointRange();
      bool worldXY = range.Low.Z == 0 && range.High.Z == 0 ? true : false;
      speckleMesh.bbox = BoxToSpeckle(range, worldXY);

      meshData.Dispose();

      return speckleMesh;
    }

    /// <summary>
    /// Given a collection of <paramref name="solids"/>, will create one <see cref="Mesh"/>
    /// </summary>
    /// <param name="solids"></param>
    /// <returns></returns>
    public Mesh GetMeshFromSolid(Element nativeSolid, string units = null)
    {
      MeshProcessor meshProcessor = new MeshProcessor();
      ElementGraphicsOutput.Process(nativeSolid, meshProcessor);
      PolyfaceHeader meshData = meshProcessor.polyfaceHeader;
      return GetMeshFromPolyfaceHeader(meshData, units);
    }

    /// <summary>
    /// Given a collection of <paramref name="nativeSolids"/>, will create one <see cref="Mesh"/>
    /// </summary>
    /// <param name="nativeSolids"></param>
    /// <returns></returns>
    public List<Mesh> GetMeshesFromSolids(IEnumerable<SolidElement> nativeSolids)
    {
      var meshes = new List<Mesh> { };
      foreach (SolidElement solid in nativeSolids)
        meshes.Add(GetMeshFromSolid(solid));
      return meshes;
    }

    /// <summary>
    /// Given <paramref name="mesh"/>, will convert and add triangle data to <paramref name="faces"/> and <paramref name="vertices"/>
    /// </summary>
    /// <param name="mesh">The revit mesh to convert</param>
    /// <param name="faces">The faces list to add to</param>
    /// <param name="vertices">The vertices list to add to</param>
    private void ConvertMeshData(Mesh mesh, List<int> faces, List<double> vertices)
    {


      int faceIndexOffset = vertices.Count / 3;

      //vertices.Capacity += mesh.Vertices.Count * 3;
      //foreach (var vert in mesh.Vertices)
      //{
      //  var (x, y, z) = Point3dToSpeckle(vert);
      //  vertices.Add(x);
      //  vertices.Add(y);
      //  vertices.Add(z);
      //}

      //faces.Capacity += mesh.NumTriangles * 4;
      //for (int i = 0; i < mesh.NumTriangles; i++)
      //{
      //  var triangle = mesh.get_Triangle(i);

      //  faces.Add(0); // TRIANGLE flag
      //  faces.Add((int)triangle.get_Index(0) + faceIndexOffset);
      //  faces.Add((int)triangle.get_Index(1) + faceIndexOffset);
      //  faces.Add((int)triangle.get_Index(2) + faceIndexOffset);
      //}
    }

    /// <summary>
    /// Helper class for a single <see cref="Objects.Geometry.Mesh"/> object for each <see cref="DB.Material"/>
    /// </summary>
    private class MeshBuildHelper
    {
      //Mesh to use for null materials (because dictionary keys can't be null)
      private Mesh nullMesh;
      //Lazy initialised Dictionary of revit material (hash) -> Speckle Mesh
      private readonly Dictionary<int, Mesh> meshMap = new Dictionary<int, Mesh>();
      public Mesh GetOrCreateMesh(Material mat, string units)
      {
        if (mat == null) return nullMesh ??= new Mesh { units = units };

        var mesh = new Mesh
        {
          units = units
        };

        return mesh;
      }

      public List<Mesh> GetAllMeshes()
      {
        List<Mesh> meshes = meshMap.Values.ToList();
        if (nullMesh != null) meshes.Add(nullMesh);
        return meshes;
      }

      public List<Mesh> GetAllValidMeshes() => GetAllMeshes().FindAll(m => m.vertices.Count > 0 && m.faces.Count > 0);

    }

    public void GetNativeProperties(Element nativeElement, Base speckleElement)
    {
      if (nativeElement == null)
        return;

      Dictionary<string, object> properties = new Dictionary<string, object>();
      using (DgnECInstanceCollection instanceCollection = GetElementProperties(nativeElement))
      {
        foreach (IDgnECInstance instance in instanceCollection)
        {
          foreach (IECPropertyValue propertyValue in instance)
          {
            if (propertyValue != null)
            {
              properties = GetValue(properties, propertyValue);
            }
          }
        }
      };

      Base elementProperties = new Base();
      foreach (string propertyName in properties.Keys)
      {
        Object value = properties[propertyName];

        if (value.GetType().Name == "DPoint3d")
        {
          elementProperties[propertyName] = ConvertToSpeckle(value);
        }
        else
        {
          elementProperties[propertyName] = value;
        }
      }
      speckleElement["properties"] = elementProperties;
    }

    public Brep SurfaceOrSolidElementToSpeckle(SurfaceOrSolidElement brep)
    {
      var _brep = new Brep();

      return _brep;
    }

  }
}

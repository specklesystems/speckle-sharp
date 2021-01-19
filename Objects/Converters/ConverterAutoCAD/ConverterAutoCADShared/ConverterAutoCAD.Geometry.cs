using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.Geometry;
using Arc = Objects.Geometry.Arc;
using Circle = Objects.Geometry.Circle;
using ControlPoint = Objects.Geometry.ControlPoint;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using Vector = Objects.Geometry.Vector;
using AC = Autodesk.AutoCAD.Geometry;

namespace Objects.Converter.AutoCAD
{
  public partial class ConverterAutoCAD
  {
    // tolerance for geometry:
    public double tolerance = 0.00;

    // Convenience methods point:
    public double[] PointToArray(Point3d pt)
    {
      return new double[] { pt.X, pt.Y, pt.Z };
    }

    public double[] PointToArray(Point2d pt)
    {
      return new double[] { pt.X, pt.Y };
    }

    // Mass point converter
    public Point3d[] PointListToNative(IEnumerable<double> arr, string units)
    {
      var enumerable = arr.ToList();
      if (enumerable.Count % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      Point3d[] points = new Point3d[enumerable.Count / 3];
      var asArray = enumerable.ToArray();
      for (int i = 2, k = 0; i < enumerable.Count; i += 3)
        points[k++] = new Point3d(
          ScaleToNative(asArray[i - 2], units),
          ScaleToNative(asArray[i - 1], units),
          ScaleToNative(asArray[i], units));

      return points;
    }

    public double[] PointsToFlatArray(IEnumerable<Point3d> points)
    {
      return points.SelectMany(pt => PointToArray(pt)).ToArray();
    }

    // Convenience methods vector:
    public double[] VectorToArray(Vector3d vc)
    {
      return new double[] { vc.X, vc.Y, vc.Z };
    }

    public Vector3d ArrayToVector(double[] arr)
    {
      return new Vector3d(arr[0], arr[1], arr[2]);
    }

    // Points
    public Point PointToSpeckle(Point3d pt)
    {
      return new Point(pt.X, pt.Y, pt.Z, ModelUnits);
    }

    public AC.Point3d PointToNative(Point pt)
    {
      var nativePt = new AC.Point3d(ScaleToNative(pt.value[0], pt.units),
        ScaleToNative(pt.value[1], pt.units),
        ScaleToNative(pt.value[2], pt.units));
      return nativePt;
    }

    // Vectors
    public Vector VectorToSpeckle(Vector3d pt)
    {
      return new Vector(pt.X, pt.Y, pt.Z, ModelUnits);
    }

    public Vector3d VectorToNative(Vector pt)
    {
      return new Vector3d(
        ScaleToNative(pt.value[0], pt.units),
        ScaleToNative(pt.value[1], pt.units),
        ScaleToNative(pt.value[2], pt.units));
    }

    // Interval
    public Interval IntervalToSpeckle(AC.Interval interval)
    {
      var speckleInterval = new Interval(interval.LowerBound, interval.UpperBound);
      return speckleInterval;
    }

    public AC.Interval IntervalToNative(Interval interval)
    {
      return new AC.Interval((double)interval.start, (double)interval.end, tolerance);
    }

    // Plane
    public Plane PlaneToSpeckle(AC.Plane plane)
    {
      // TODO: get x and y axis form coefficients
      var xAxis = new Vector();
      var yAxis = new Vector();
      return new Plane(PointToSpeckle(plane.PointOnPlane), VectorToSpeckle(plane.Normal), yAxis,
        yAxis, ModelUnits);
    }

    public AC.Plane PlaneToNative(Plane plane)
    {
      return new AC.Plane(PointToNative(plane.origin), VectorToNative(plane.normal));
    }

    // Line
    public Line LineToSpeckle(AC.Line3d line)
    {
      var sLine = new Line(PointsToFlatArray(new Point3d[] { line.StartPoint, line.EndPoint }), ModelUnits);

      return sLine;
    }
    public AC.Line3d LineToNative(Line line)
    {
      return null;
    }

    public Polyline PolylineToSpeckle(AC.PolylineCurve3d polyline)
    {
      return null;
    }

    public PolylineCurve3d PolylineToNative(Polyline polyline)
    {
      return null;
    }

    // Curve
    public AC.Curve3d CurveToNative(ICurve curve)
    {
      switch (curve)
      {
        case Circle circle:
          return null;

        case Arc arc:
          return null;

        case Ellipse ellipse:
          return null;

        case Curve crv:
          return NurbsToNative(crv);

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

    public ICurve CurveToSpeckle(AC.Curve3d curve)
    {
      if (curve.IsPlanar(out AC.Plane pln))
      {
        if (curve.IsPeriodic(tolerance) && curve.IsClosed)
        {
          curve.TryGetCircle(out var getObj, tolerance);
          var cir = CircleToSpeckle(getObj);
          cir.domain = IntervalToSpeckle(curve.Domain);
          return cir;
        }

        if (curve.IsArc(tolerance))
        {
          curve.TryGetArc(out var getObj, tolerance);
          var arc = ArcToSpeckle(getObj);
          arc.domain = IntervalToSpeckle(curve.Domain);
          return arc;
        }

        if (curve.IsEllipse(tolerance) && curve.IsClosed)
        {
          curve.TryGetEllipse(pln, out var getObj, tolerance);
          var ellipse = EllipseToSpeckle(getObj);
          ellipse.domain = IntervalToSpeckle(curve.Domain);
        }

        if (curve.IsLinear(tolerance) || curve.IsPolyline()) // defaults to polyline
        {
          curve.TryGetPolyline(out var getObj);
          if (null != getObj)
          {
            return PolylineToSpeckle(getObj, IntervalToSpeckle(curve.Domain));
          }
        }
      }

      return NurbsToSpeckle(curve as NurbCurve3d);
    }

    public Curve NurbsToSpeckle(AC.NurbCurve3d curve)
    {
      var tolerance = 0.0;

      curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out var poly);

      Polyline displayValue;

      if (poly.Count == 2)
      {
        displayValue = new Polyline();
        displayValue.value = new List<double> { poly[0].X, poly[0].Y, poly[0].Z, poly[1].X, poly[1].Y, poly[1].Z };
      }
      else
      {
        displayValue = PolylineToSpeckle(poly) as Polyline;
      }

      var myCurve = new Curve(displayValue, ModelUnits);
      var nurbsCurve = curve.ToNurbsCurve();

      myCurve.weights = nurbsCurve.Points.Select(ctp => ctp.Weight).ToList();
      myCurve.points = PointsToFlatArray(nurbsCurve.Points.Select(ctp => ctp.Location)).ToList();
      myCurve.knots = nurbsCurve.Knots.ToList();
      myCurve.degree = nurbsCurve.Degree;
      myCurve.periodic = nurbsCurve.IsPeriodic;
      myCurve.rational = nurbsCurve.IsRational;
      myCurve.domain = IntervalToSpeckle(nurbsCurve.Domain);
      myCurve.closed = nurbsCurve.IsClosed;

      return myCurve;
    }

    public NurbCurve3d NurbsToNative(Curve curve)
    {
      var ptsList = PointListToNative(curve.points, curve.units);

      var nurbsCurve = NurbCurve3d.Create(false, curve.degree, ptsList);

      for (int j = 0; j < nurbsCurve.Points.Count; j++)
      {
        nurbsCurve.Points.SetPoint(j, ptsList[j], curve.weights[j]);
      }

      for (int j = 0; j < nurbsCurve.Knots.Count; j++)
      {
        nurbsCurve.Knots[j] = curve.knots[j];
      }

      nurbsCurve.Domain = IntervalToNative(curve.domain ?? new Interval(0, 1));
      return nurbsCurve;
    }

    public AC.NurbSurface SurfaceToNative(Geometry.Surface surface)
    {
      // Create ac surface
      var points = surface.GetControlPoints().Select(l => l.Select(p =>
        new ControlPoint(
          ScaleToNative(p.x, p.units),
          ScaleToNative(p.y, p.units),
          ScaleToNative(p.z, p.units),
          p.weight,
          p.units)).ToList()).ToList();

      var result = AC.NurbSurface.Create(3, surface.rational, surface.degreeU + 1, surface.degreeV + 1,
        points.Count, points[0].Count);

      // Set knot vectors
      for (int i = 0; i < surface.knotsU.Count; i++)
      {
        result.KnotsU[i] = surface.knotsU[i];
      }

      for (int i = 0; i < surface.knotsV.Count; i++)
      {
        result.KnotsV[i] = surface.knotsV[i];
      }

      // Set control points
      for (var i = 0; i < points.Count; i++)
      {
        for (var j = 0; j < points[i].Count; j++)
        {
          var pt = points[i][j];
          result.Points.SetPoint(i, j, pt.x * pt.weight, pt.y * pt.weight, pt.z * pt.weight);
          result.Points.SetWeight(i, j, pt.weight);
        }
      }

      // Return surface
      return result;
    }

    public Geometry.Surface SurfaceToSpeckle(AC.NurbSurface surface)
    {
      var result = new Geometry.Surface
      {
        degreeU = surface.OrderU - 1,
        degreeV = surface.OrderV - 1,
        rational = surface.IsRational,
        closedU = surface.IsClosed(0),
        closedV = surface.IsClosed(1),
        domainU = IntervalToSpeckle(surface.Domain(0)),
        domainV = IntervalToSpeckle(surface.Domain(1)),
        knotsU = surface.KnotsU.ToList(),
        knotsV = surface.KnotsV.ToList()
      };
      result.units = ModelUnits;

      result.SetControlPoints(ControlPointsToSpeckle(surface.Points));
      return result;
    }
  }
}
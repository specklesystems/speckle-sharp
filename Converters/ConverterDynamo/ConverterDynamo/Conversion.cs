using Autodesk.DesignScript.Geometry;
using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using System.Runtime.CompilerServices;
using DS = Autodesk.DesignScript.Geometry;
using Objects.Primitive;
using Point = Objects.Geometry.Point;
using Vector = Objects.Geometry.Vector;
using Line = Objects.Geometry.Line;
using Plane = Objects.Geometry.Plane;
using Circle = Objects.Geometry.Circle;
using Arc = Objects.Geometry.Arc;
using Ellipse = Objects.Geometry.Ellipse;
using Curve = Objects.Geometry.Curve;
using Mesh = Objects.Geometry.Mesh;
using Objects;

namespace Objects.Converter.Dynamo
{
  //Original author 
  //     Name: Alvaro Ortega Pickmans 
  //     Github: alvpickmans
  //Code form: https://github.com/speckleworks/SpeckleCoreGeometry/blob/master/SpeckleCoreGeometryDynamo/Conversions.cs
  static class Conversion
  {

    private const double EPS = 1e-6;
    private const string speckleKey = "speckle";

    #region Points
    /// <summary>
    /// DS Point to SpecklePoint
    /// </summary>
    /// <param name="pt"></param>
    /// <returns></returns>
    public static Point ToSpeckle(this DS.Point pt)
    {
      var point = new Point(pt.X, pt.Y, pt.Z);
      point.SetDynamicMembers(pt.GetSpeckleProperties());
      return point;
    }

    /// <summary>
    /// Speckle Point to DS Point
    /// </summary>
    /// <param name="pt"></param>
    /// <returns></returns>
    /// 
    public static DS.Point ToNative(this Point pt)
    {
      var point = DS.Point.ByCoordinates(pt.value[0], pt.value[1], pt.value[2]);

      return point.SetDynamoProperties<DS.Point>(pt.GetDynamicMembersDictionary());
    }


    /// <summary>
    /// Array of point coordinates to array of DS Points
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static DS.Point[] ToPoints(this IEnumerable<double> arr)
    {
      if (arr.Count() % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      DS.Point[] points = new DS.Point[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
        points[k++] = DS.Point.ByCoordinates(asArray[i - 2], asArray[i - 1], asArray[i]);

      return points;
    }

    /// <summary>
    /// Array of DS Points to array of point coordinates
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static double[] ToFlatArray(this IEnumerable<DS.Point> points)
    {
      return points.SelectMany(pt => pt.ToArray()).ToArray();
    }

    #endregion

    #region Vectors
    /// <summary>
    /// DS Vector to Vector
    /// </summary>
    /// <param name="vc"></param>
    /// <returns></returns>
    public static Vector ToSpeckle(this DS.Vector vc)
    {
      return new Vector(vc.X, vc.Y, vc.Z);
    }

    /// <summary>
    /// Vector to DS Vector
    /// </summary>
    /// <param name="vc"></param>
    /// <returns></returns>
    public static DS.Vector ToNative(this Vector vc)
    {
      return DS.Vector.ByCoordinates(vc.value[0], vc.value[1], vc.value[2]);
    }

    /// <summary>
    /// DS Vector to array of coordinates
    /// </summary>
    /// <param name="vc"></param>
    /// <returns></returns>
    public static double[] ToArray(this DS.Vector vc)
    {
      return new double[] { vc.X, vc.Y, vc.Z };
    }

    /// <summary>
    /// Array of coordinates to DS Vector
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static DS.Vector ToVector(this double[] arr)
    {
      return DS.Vector.ByCoordinates(arr[0], arr[1], arr[2]);
    }
    #endregion


    #region Planes
    /// <summary>
    /// DS Plane to Plane
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    public static Plane ToSpeckle(this DS.Plane plane)
    {
      var p = new Plane(
      plane.Origin.ToSpeckle(),
      plane.Normal.ToSpeckle(),
      plane.XAxis.ToSpeckle(),
      plane.YAxis.ToSpeckle());
      p.SetDynamicMembers(plane.GetSpeckleProperties());
      return p;
    }

    /// <summary>
    /// Plane to DS Plane
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    public static DS.Plane ToNative(this Plane plane)
    {
      var pln = DS.Plane.ByOriginXAxisYAxis(
        plane.origin.ToNative(),
        plane.xdir.ToNative(),
        plane.ydir.ToNative());

      return pln.SetDynamoProperties<DS.Plane>(plane.GetDynamicMembersDictionary());
    }
    #endregion

    #region Linear
    /// <summary>
    /// DS Line to SpeckleLine
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Line ToSpeckle(this DS.Line line)
    {
      var l = new Line(
        (new DS.Point[] { line.StartPoint, line.EndPoint }).ToFlatArray());
      l.SetDynamicMembers(line.GetSpeckleProperties());
      return l;
    }

    /// <summary>
    /// SpeckleLine to DS Line
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static DS.Line ToNative(this Line line)
    {
      var pts = line.value.ToPoints();
      var ln = DS.Line.ByStartPointEndPoint(pts[0], pts[1]);

      pts.ForEach(pt => pt.Dispose());

      return ln.SetDynamoProperties<DS.Line>(line.GetDynamicMembersDictionary());
    }

    /// <summary>
    /// DS Polygon to closed SpecklePolyline
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns></returns>
    public static Polyline ToSpeckle(this DS.Polygon polygon)
    {
      var poly = new Polyline(polygon.Points.ToFlatArray(), null)
      {
        closed = true,
      };
      poly.SetDynamicMembers(polygon.GetSpeckleProperties());

      return poly;
    }


    /// <summary>
    /// DS Rectangle to SpecklePolyline
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Polyline ToSpeckle(this DS.Rectangle rectangle)
    {
      var rect = (rectangle as DS.Polygon).ToSpeckle();
      rect.SetDynamicMembers(rectangle.GetSpeckleProperties());
      return rect;
    }

    /// <summary>
    /// SpecklePolyline to DS Rectangle if closed , four points and sides parallel; 
    /// DS Polygon if closed or DS Polycurve otherwise
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    public static DS.Curve ToNative(this Polyline polyline)
    {
      var points = polyline.value.ToPoints();
      if (polyline.closed) return DS.PolyCurve.ByPoints(points).CloseWithLine().SetDynamoProperties<DS.PolyCurve>(polyline.GetDynamicMembersDictionary());

      return PolyCurve.ByPoints(points).SetDynamoProperties<PolyCurve>(polyline.GetDynamicMembersDictionary());
    }

    #endregion


    #region Curves Helper Methods

    public static bool IsLinear(this DS.Curve curve)
    {
      try
      {
        if (curve.IsClosed) { return false; }
        //Dynamo cannot be trusted when less than 1e-6
        var extremesDistance = curve.StartPoint.DistanceTo(curve.EndPoint);
        return Threshold(curve.Length, extremesDistance);
      }
      catch (Exception e)
      {
        return false;
      }
    }

    public static DS.Line GetAsLine(this DS.Curve curve)
    {
      if (curve.IsClosed) { throw new ArgumentException("Curve is closed, cannot be a Line"); }
      return DS.Line.ByStartPointEndPoint(curve.StartPoint, curve.EndPoint);
    }

    public static bool IsPolyline(this PolyCurve polycurve)
    {
      return polycurve.Curves().All(c => c.IsLinear());
    }

    public static bool IsArc(this DS.Curve curve)
    {
      try
      {
        if (curve.IsClosed) { return false; }
        using (DS.Point midPoint = curve.PointAtParameter(0.5))
        using (DS.Arc arc = DS.Arc.ByThreePoints(curve.StartPoint, midPoint, curve.EndPoint))
        {
          return Threshold(arc.Length, curve.Length);
        }
      }
      catch (Exception e)
      {
        return false;
      }
    }

    public static DS.Arc GetAsArc(this DS.Curve curve)
    {
      if (curve.IsClosed) { throw new ArgumentException("Curve is closed, cannot be an Arc"); }
      using (DS.Point midPoint = curve.PointAtParameter(0.5))
      {
        return DS.Arc.ByThreePoints(curve.StartPoint, midPoint, curve.EndPoint);
      }

    }

    public static bool IsCircle(this DS.Curve curve)
    {
      try
      {
        if (!curve.IsClosed) { return false; }
        using (DS.Point midPoint = curve.PointAtParameter(0.5))
        {
          double radius = curve.StartPoint.DistanceTo(midPoint) * 0.5;
          return Threshold(radius, (curve.Length) / (2 * Math.PI));
        }
      }
      catch (Exception e)
      {
        return false;
      }
    }

    public static DS.Circle GetAsCircle(this DS.Curve curve)
    {
      if (!curve.IsClosed) { throw new ArgumentException("Curve is not closed, cannot be a Circle"); }

      DS.Point start = curve.StartPoint;
      using (DS.Point midPoint = curve.PointAtParameter(0.5))
      using (DS.Point centre = DS.Point.ByCoordinates(Median(start.X, midPoint.X), Median(start.Y, midPoint.Y), Median(start.Z, midPoint.Z)))
      {
        return DS.Circle.ByCenterPointRadiusNormal(
            centre,
            centre.DistanceTo(start),
            curve.Normal
        );
      }
    }

    public static bool IsEllipse(this DS.Curve curve)
    {
      try
      {
        if (!curve.IsClosed) { return false; }

        //http://www.numericana.com/answer/ellipse.htm
        double[] parameters = new double[4] { 0, 0.25, 0.5, 0.75 };
        DS.Point[] points = parameters.Select(p => curve.PointAtParameter(p)).ToArray();
        double a = points[0].DistanceTo(points[2]) * 0.5; // Max Radius
        double b = points[1].DistanceTo(points[3]) * 0.5; // Min Radius
        points.ForEach(p => p.Dispose());

        double h = Math.Pow(a - b, 2) / Math.Pow(a + b, 2);
        double perimeter = Math.PI * (a + b) * (1 + (3 * h / (10 + Math.Sqrt(4 - 3 * h))));

        return Threshold(curve.Length, perimeter, 1e-5); //Ellipse perimeter is an approximation
      }
      catch (Exception e)
      {
        return false;
      }
    }

    public static DS.Ellipse GetAsEllipse(this DS.Curve curve)
    {
      if (!curve.IsClosed) { throw new ArgumentException("Curve is not closed, cannot be an Ellipse"); }
      double[] parameters = new double[4] { 0, 0.25, 0.5, 0.75 };
      DS.Point[] points = parameters.Select(p => curve.PointAtParameter(p)).ToArray();
      double a = points[0].DistanceTo(points[2]) * 0.5; // Max Radius
      double b = points[1].DistanceTo(points[3]) * 0.5; // Min Radius

      using (DS.Point centre = DS.Point.ByCoordinates(Median(points[0].X, points[2].X), Median(points[0].Y, points[2].Y), Median(points[0].Z, points[2].Z)))
      {
        points.ForEach(p => p.Dispose());

        return DS.Ellipse.ByPlaneRadii(
            DS.Plane.ByOriginNormalXAxis(centre, curve.Normal, DS.Vector.ByTwoPoints(centre, curve.StartPoint)),
            a,
            b
            );
      }
    }

    #endregion


    #region Curves

    /// <summary>
    /// DS Circle to SpeckleCircle.
    /// </summary>
    /// <param name="circ"></param>
    /// <returns></returns>
    public static Circle ToSpeckle(this DS.Circle circ)
    {
      using (DS.Vector xAxis = DS.Vector.ByTwoPoints(circ.CenterPoint, circ.StartPoint))
      using (DS.Plane plane = DS.Plane.ByOriginNormalXAxis(circ.CenterPoint, circ.Normal, xAxis))
      {
        var myCircle = new Circle(plane.ToSpeckle(), circ.Radius);
        myCircle.SetDynamicMembers(circ.GetSpeckleProperties());

        return myCircle;
      }
    }

    /// <summary>
    /// SpeckleCircle to DS Circle. Rotating the circle is due to a bug in ProtoGeometry
    /// that will be solved on Dynamo 2.1.
    /// </summary>
    /// <param name="circ"></param>
    /// <returns></returns>
    public static DS.Circle ToNative(this Circle circ)
    {
      using (DS.Plane basePlane = circ.plane.ToNative())
      using (DS.Circle preCircle = DS.Circle.ByPlaneRadius(basePlane, circ.radius.Value))
      using (DS.Vector preXvector = DS.Vector.ByTwoPoints(preCircle.CenterPoint, preCircle.StartPoint))
      {
        double angle = preXvector.AngleAboutAxis(basePlane.XAxis, basePlane.Normal);
        var circle = (DS.Circle)preCircle.Rotate(basePlane, angle);

        return circle.SetDynamoProperties<DS.Circle>(circ.GetDynamicMembersDictionary());
      }
    }

    /// <summary>
    /// DS Arc to SpeckleArc
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static Arc ToSpeckle(this DS.Arc a)
    {
      using (DS.Vector xAxis = DS.Vector.ByTwoPoints(a.CenterPoint, a.StartPoint))
      using (DS.Plane basePlane = DS.Plane.ByOriginNormalXAxis(a.CenterPoint, a.Normal, xAxis))
      {
        var arc = new Arc(
            basePlane.ToSpeckle(),
            a.Radius,
            0, // This becomes 0 as arcs are interpreted to start from the plane's X axis.
            a.SweepAngle.ToRadians(),
            a.SweepAngle.ToRadians()
            );

        arc.SetDynamicMembers(a.GetSpeckleProperties());
        return arc;
      }
    }

    /// <summary>
    /// SpeckleArc to DS Arc
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static DS.Arc ToNative(this Arc a)
    {
      using (DS.Plane basePlane = a.plane.ToNative())
      using (DS.Point startPoint = (DS.Point)basePlane.Origin.Translate(basePlane.XAxis, a.radius.Value))
      {
        var arc = DS.Arc.ByCenterPointStartPointSweepAngle(
            basePlane.Origin,
            startPoint,
            a.angleRadians.Value.ToDegrees(),
            basePlane.Normal
          );
        return arc.SetDynamoProperties<DS.Arc>(a.GetDynamicMembersDictionary());
      }
    }

    /// <summary>
    /// DS Ellipse to SpeckleEllipse
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static Ellipse ToSpeckle(this DS.Ellipse e)
    {
      using (DS.Plane basePlane = DS.Plane.ByOriginNormalXAxis(e.CenterPoint, e.Normal, e.MajorAxis))
      {
        var ellipse = new Ellipse(
          basePlane.ToSpeckle(),
          e.MajorAxis.Length,
          e.MinorAxis.Length);
        ellipse.SetDynamicMembers(e.GetSpeckleProperties());

        return ellipse;
      }
    }

    /// <summary>
    /// SpeckleEllipse to DS Ellipse
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static DS.Ellipse ToNative(this Ellipse e)
    {
      var ellipse = DS.Ellipse.ByPlaneRadii(
          e.plane.ToNative(),
          e.firstRadius.Value,
          e.secondRadius.Value
      );
      return ellipse.SetDynamoProperties<DS.Ellipse>(e.GetDynamicMembersDictionary());
    }

    /// <summary>
    /// DS EllipsArc to SpeckleCurve?????
    /// </summary>
    /// <param name="arc"></param>
    /// <returns></returns>
    public static Base ToSpeckle(this EllipseArc arc)
    {
      //EllipseArcs as NurbsCurves
      using (NurbsCurve nurbsCurve = arc.ToNurbsCurve())
      {
        var nurbs = nurbsCurve.ToSpeckle();
        nurbs.SetDynamicMembers(arc.GetSpeckleProperties());
        return nurbs;
      }
    }

    //public static EllipseArc ToNative(this SpeckleCurve arc)
    //{
    //  //TODO: Implement EllipseArc converter
    //  throw new NotImplementedException("EllipsArc not implemented yet.");
    //}

    /// <summary>
    /// DS Polycurve to SpecklePolyline if all curves are linear
    /// SpecklePolycurve otherwise
    /// </summary>
    /// <param name="polycurve"></param>
    /// <returns name="speckleObject"></returns>
    public static Base ToSpeckle(this PolyCurve polycurve)
    {
      if (polycurve.IsPolyline())
      {
        var points = polycurve.Curves().SelectMany(c => c.StartPoint.ToArray()).ToList();
        points.AddRange(polycurve.Curves().Last().EndPoint.ToArray());
        var poly = new Polyline(points);

        poly.SetDynamicMembers(polycurve.GetSpeckleProperties());
        return poly;
      }
      else
      {
        Polycurve spkPolycurve = new Polycurve();
        spkPolycurve.SetDynamicMembers(polycurve.GetSpeckleProperties());
        spkPolycurve.segments = polycurve.Curves().Select(c => (ICurve)c.ToSpeckle()).ToList();

        return spkPolycurve;
      }
    }

    public static PolyCurve ToNative(this Polycurve polycurve)
    {
      DS.Curve[] curves = new DS.Curve[polycurve.segments.Count];
      for (var i = 0; i < polycurve.segments.Count; i++)
      {
        switch (polycurve.segments[i])
        {
          case Line curve:
            curves[i] = curve.ToNative();
            break;
          case Arc curve:
            curves[i] = curve.ToNative();
            break;
          case Circle curve:
            curves[i] = curve.ToNative();
            break;
          case Ellipse curve:
            curves[i] = curve.ToNative();
            break;
          case Polycurve curve:
            curves[i] = curve.ToNative();
            break;
          case Polyline curve:
            curves[i] = curve.ToNative();
            break;
          case Curve curve:
            curves[i] = curve.ToNative();
            break;
        }
      }
      var polyCrv = PolyCurve.ByJoinedCurves(curves);
      return polyCrv.SetDynamoProperties<PolyCurve>(polycurve.GetDynamicMembersDictionary());
    }

    public static Base ToSpeckle(this DS.Curve curve)
    {
      Base speckleCurve;
      if (curve.IsLinear())
      {
        using (DS.Line line = curve.GetAsLine()) { speckleCurve = line.ToSpeckle(); }
      }
      else if (curve.IsArc())
      {
        using (DS.Arc arc = curve.GetAsArc()) { speckleCurve = arc.ToSpeckle(); }
      }
      else if (curve.IsCircle())
      {
        using (DS.Circle circle = curve.GetAsCircle()) { speckleCurve = circle.ToSpeckle(); }
      }
      else if (curve.IsEllipse())
      {
        using (DS.Ellipse ellipse = curve.GetAsEllipse()) { speckleCurve = ellipse.ToSpeckle(); }
      }
      else
      {
        speckleCurve = curve.ToNurbsCurve().ToSpeckle();
      }

      speckleCurve.SetDynamicMembers(curve.GetSpeckleProperties());
      return speckleCurve;
    }

    public static NurbsCurve ToNative(this Curve curve)
    {
      var points = curve.points.ToPoints();
      var dsKnots = curve.knots;
      dsKnots.Insert(0, dsKnots.First());
      dsKnots.Add(dsKnots.Last());

      NurbsCurve nurbsCurve = NurbsCurve.ByControlPointsWeightsKnots(
          points,
          curve.weights.ToArray(),
          dsKnots.ToArray(),
          curve.degree
          );

      return nurbsCurve.SetDynamoProperties<NurbsCurve>(curve.GetDynamicMembersDictionary());
    }

    public static Base ToSpeckle(this NurbsCurve curve)
    {
      Base speckleCurve;
      if (curve.IsLinear())
      {
        using (DS.Line line = curve.GetAsLine()) { speckleCurve = line.ToSpeckle(); }
      }
      else if (curve.IsArc())
      {
        using (DS.Arc arc = curve.GetAsArc()) { speckleCurve = arc.ToSpeckle(); }
      }
      else if (curve.IsCircle())
      {
        using (DS.Circle circle = curve.GetAsCircle()) { speckleCurve = circle.ToSpeckle(); }
      }
      else if (curve.IsEllipse())
      {
        using (DS.Ellipse ellipse = curve.GetAsEllipse()) { speckleCurve = ellipse.ToSpeckle(); }
      }
      else
      {
        // SpeckleCurve DisplayValue
        DS.Curve[] curves = curve.ApproximateWithArcAndLineSegments();
        List<double> polylineCoordinates = curves.SelectMany(c => new DS.Point[2] { c.StartPoint, c.EndPoint }.ToFlatArray()).ToList();
        polylineCoordinates.AddRange(curves.Last().EndPoint.ToArray());
        curves.ForEach(c => c.Dispose());

        Polyline displayValue = new Polyline(polylineCoordinates);
        List<double> dsKnots = curve.Knots().ToList();
        dsKnots.RemoveAt(dsKnots.Count - 1);
        dsKnots.RemoveAt(0);

        Curve spkCurve = new Curve(displayValue);
        spkCurve.weights = curve.Weights().ToList();
        spkCurve.points = curve.ControlPoints().ToFlatArray().ToList();
        spkCurve.knots = dsKnots;
        spkCurve.degree = curve.Degree;
        spkCurve.periodic = curve.IsPeriodic;
        spkCurve.rational = curve.IsRational;
        spkCurve.closed = curve.IsClosed;
        spkCurve.domain = new Interval(curve.StartParameter(), curve.EndParameter());
        //spkCurve.Properties

        //spkCurve.GenerateHash();

        speckleCurve = spkCurve;
      }
      speckleCurve.SetDynamicMembers(curve.GetSpeckleProperties());
      return speckleCurve;

    }

    public static Base ToSpeckle(this Helix helix)
    {
      using (NurbsCurve nurbsCurve = helix.ToNurbsCurve())
      {
        var curve = nurbsCurve.ToSpeckle();
        curve.SetDynamicMembers(helix.GetSpeckleProperties());
        return curve;
      }
    }

    #endregion

    #region mesh

    public static DS.Mesh ToNative(this Brep brep)
    {
      if (brep.displayValue != null)
      {
        return brep.displayValue.ToNative();
      }
      return null;
    }

    // Meshes
    public static Mesh ToSpeckle(this DS.Mesh mesh)
    {
      var vertices = mesh.VertexPositions.ToFlatArray();
      var defaultColour = System.Drawing.Color.FromArgb(255, 100, 100, 100);

      var faces = mesh.FaceIndices.SelectMany(f =>
      {
        if (f.Count == 4) { return new int[5] { 1, (int)f.A, (int)f.B, (int)f.C, (int)f.D }; }
        else { return new int[4] { 0, (int)f.A, (int)f.B, (int)f.C }; }
      })
      .ToArray();

      var colors = Enumerable.Repeat(defaultColour.ToArgb(), vertices.Count()).ToArray();
      //double[] textureCoords;

      //if (SpeckleRhinoConverter.AddMeshTextureCoordinates)
      //{
      //  textureCoords = mesh.TextureCoordinates.Select(pt => pt).ToFlatArray();
      //  return new SpeckleMesh(vertices, faces, Colors, textureCoords, properties: mesh.UserDictionary.ToSpeckle());
      //}

      var speckleMesh = new Mesh(vertices, faces, colors, null);
      speckleMesh.SetDynamicMembers(mesh.GetSpeckleProperties());

      return speckleMesh;
    }

    public static DS.Mesh ToNative(this Mesh mesh)
    {
      var points = mesh.vertices.ToPoints();
      List<IndexGroup> faces = new List<IndexGroup>();
      int i = 0;

      while (i < mesh.faces.Count)
      {
        if (mesh.faces[i] == 0)
        { // triangle
          var ig = IndexGroup.ByIndices((uint)mesh.faces[i + 1], (uint)mesh.faces[i + 2], (uint)mesh.faces[i + 3]);
          faces.Add(ig);
          i += 4;
        }
        else
        { // quad
          var ig = IndexGroup.ByIndices((uint)mesh.faces[i + 1], (uint)mesh.faces[i + 2], (uint)mesh.faces[i + 3], (uint)mesh.faces[i + 4]);
          faces.Add(ig);
          i += 5;
        }
      }

      var dsMesh = DS.Mesh.ByPointsFaceIndices(points, faces);
      
      dsMesh.SetDynamoProperties<DS.Mesh>(mesh.GetDynamicMembersDictionary());
      
      return dsMesh;
    }
    #endregion

    #region Helper Methods

    public static double[] ToArray(this DS.Point pt)
    {
      return new double[] { pt.X, pt.Y, pt.Z };
    }

    public static DS.Point ToPoint(this double[] arr)
    {
      return DS.Point.ByCoordinates(arr[0], arr[1], arr[2]);
    }

    public static double ToDegrees(this double radians)
    {
      return radians * (180 / Math.PI);
    }

    public static double ToRadians(this double degrees)
    {
      return degrees * (Math.PI / 180);
    }

    public static bool Threshold(double value1, double value2, double error = EPS)
    {
      return Math.Abs(value1 - value2) <= error;
    }

    public static double Median(double min, double max)
    {
      return ((max - min) * 0.5) + min;
    }

    /// SpeckleCore does not currently support dictionaries, therofere avoiding the canonical ToSpeckle
    public static Dictionary<string, object> ToSpeckleX(this DesignScript.Builtin.Dictionary dict)
    {
      if (dict == null) { return null; }
      var speckleDict = new Dictionary<string, object>();
      foreach (var key in dict.Keys)
      {
        object value = dict.ValueAtKey(key);
        if (value is DesignScript.Builtin.Dictionary)
        {
          value = (value as DesignScript.Builtin.Dictionary).ToSpeckleX();
        }
        //TODO:
        //else if (value is Geometry)
        //{
        //  value = Converter.Serialise(value);
        //}
        speckleDict.Add(key, value);
      }
      return speckleDict;
    }

    /// SpeckleCore does not currently support dictionaries, therofere avoiding the canonical ToNative
    public static DesignScript.Builtin.Dictionary ToNativeX(this Dictionary<string, object> speckleDict)
    {
      if (speckleDict == null) { return null; }
      var keys = new List<string>();
      var values = new List<object>();
      foreach (var pair in speckleDict)
      {
        object value = pair.Value;
        if (value is Dictionary<string, object>)
        {
          value = (value as Dictionary<string, object>).ToNativeX();
        }
        //else if (value is Base)
        //{
        //  value = Converter.Deserialise(value as Base);
        //}
        keys.Add(pair.Key);
        values.Add(value);
      }
      return DesignScript.Builtin.Dictionary.ByKeysValues(keys, values);
    }

    public static Dictionary<string, object> GetSpeckleProperties(this DesignScriptEntity geometry)
    {
      var userData = geometry.Tags.LookupTag(speckleKey) as DesignScript.Builtin.Dictionary;
      if (userData == null)
        return new Dictionary<string, object>();
      return userData.ToSpeckleX(); ;
    }

    public static T SetDynamoProperties<T>(this DesignScriptEntity geometry, Dictionary<string, object> properties)
    {
      if (properties != null)
      {
        geometry.Tags.AddTag(speckleKey, properties.ToNativeX());
      }
      return (T)Convert.ChangeType(geometry, typeof(T));
    }

    #endregion
  }
}

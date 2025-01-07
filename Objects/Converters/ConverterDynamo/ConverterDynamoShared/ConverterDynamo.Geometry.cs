using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Geometry;
using Objects.Geometry;
using Objects.Other;
using Objects.Primitive;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Arc = Objects.Geometry.Arc;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using DS = Autodesk.DesignScript.Geometry;
using Ellipse = Objects.Geometry.Ellipse;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Spiral = Objects.Geometry.Spiral;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Dynamo;

//Original author
//     Name: Alvaro Ortega Pickmans
//     Github: alvpickmans
//Code form: https://github.com/speckleworks/SpeckleCoreGeometry/blob/master/SpeckleCoreGeometryDynamo/Conversions.cs
public partial class ConverterDynamo
{
  #region Points

  /// <summary>
  /// DS Point to SpecklePoint
  /// </summary>
  /// <param name="pt"></param>
  /// <returns></returns>
  public Point PointToSpeckle(DS.Point pt, string units = null)
  {
    var point = new Point(pt.X, pt.Y, pt.Z, units ?? ModelUnits);
    CopyProperties(point, pt);
    return point;
  }

  /// <summary>
  /// Speckle Point to DS Point
  /// </summary>
  /// <param name="pt"></param>
  /// <returns></returns>
  ///
  public DS.Point PointToNative(Point pt)
  {
    var point = DS.Point.ByCoordinates(
      ScaleToNative(pt.x, pt.units),
      ScaleToNative(pt.y, pt.units),
      ScaleToNative(pt.z, pt.units)
    );

    return point.SetDynamoProperties<DS.Point>(GetDynamicMembersFromBase(pt));
  }

  /// <summary>
  /// Array of point coordinates to array of DS Points
  /// </summary>
  /// <param name="arr"></param>
  /// <returns></returns>
  public DS.Point[] ArrayToPointList(IEnumerable<double> arr, string units = null)
  {
    if (arr.Count() % 3 != 0)
    {
      throw new SpeckleException("Array malformed: length%3 != 0.");
    }

    DS.Point[] points = new DS.Point[arr.Count() / 3];
    var asArray = arr.ToArray();
    for (int i = 2, k = 0; i < arr.Count(); i += 3)
    {
      points[k++] = DS.Point.ByCoordinates(
        ScaleToNative(asArray[i - 2], units),
        ScaleToNative(asArray[i - 1], units),
        ScaleToNative(asArray[i], units)
      );
    }

    return points;
  }

  /// <summary>
  /// Array of DS Points to array of point coordinates
  /// </summary>
  /// <param name="points"></param>
  /// <returns></returns>
  public List<double> PointListToFlatList(IEnumerable<DS.Point> points)
  {
    return points.SelectMany(pt => PointToArray(pt)).ToList();
  }

  public double[] PointToArray(DS.Point pt)
  {
    return new double[] { pt.X, pt.Y, pt.Z };
  }

  #endregion

  #region Vectors

  /// <summary>
  /// DS Vector to Vector
  /// </summary>
  /// <param name="vc"></param>
  /// <returns></returns>
  public Vector VectorToSpeckle(DS.Vector vc, string units = null)
  {
    return new Vector(vc.X, vc.Y, vc.Z, units ?? ModelUnits);
  }

  /// <summary>
  /// Vector to DS Vector
  /// </summary>
  /// <param name="vc"></param>
  /// <returns></returns>
  public DS.Vector VectorToNative(Vector vc)
  {
    return DS.Vector.ByCoordinates(
      ScaleToNative(vc.x, vc.units),
      ScaleToNative(vc.y, vc.units),
      ScaleToNative(vc.z, vc.units)
    );
  }

  /// <summary>
  /// DS Vector to array of coordinates
  /// </summary>
  /// <param name="vc"></param>
  /// <returns></returns>
  //public double[] VectorToArray(DS.Vector vc)
  //{
  //  return new double[] { vc.X, vc.Y, vc.Z };
  //}

  /// <summary>
  /// Array of coordinates to DS Vector
  /// </summary>
  /// <param name="arr"></param>
  /// <returns></returns>
  //public DS.Vector VectorToVector(double[] arr)
  //{
  //  return DS.Vector.ByCoordinates(arr[0], arr[1], arr[2]);
  //}

  #endregion

  #region Planes

  /// <summary>
  /// DS Plane to Plane
  /// </summary>
  /// <param name="plane"></param>
  /// <returns></returns>
  public Plane PlaneToSpeckle(DS.Plane plane, string units = null)
  {
    var u = units ?? ModelUnits;
    var p = new Plane(
      PointToSpeckle(plane.Origin, u),
      VectorToSpeckle(plane.Normal, u),
      VectorToSpeckle(plane.XAxis, u),
      VectorToSpeckle(plane.YAxis, u),
      ModelUnits
    );
    CopyProperties(p, plane);
    return p;
  }

  /// <summary>
  /// Plane to DS Plane
  /// </summary>
  /// <param name="plane"></param>
  /// <returns></returns>
  public DS.Plane PlaneToNative(Plane plane)
  {
    var pln = DS.Plane.ByOriginXAxisYAxis(
      PointToNative(plane.origin),
      VectorToNative(plane.xdir),
      VectorToNative(plane.ydir)
    );
    return pln.SetDynamoProperties<DS.Plane>(GetDynamicMembersFromBase(plane));
  }

  public CoordinateSystem TransformToNative(Transform transform)
  {
    return CoordinateSystem.ByMatrix(transform.ToArray()).Scale(Units.GetConversionFactor(transform.units, ModelUnits));
  }

  #endregion

  #region Linear

  /// <summary>
  /// DS Line to SpeckleLine
  /// </summary>
  /// <param name="line"></param>
  /// <returns></returns>
  public Line LineToSpeckle(DS.Line line, string units = null)
  {
    var u = units ?? ModelUnits;
    var l = new Line(PointListToFlatList(new DS.Point[] { line.StartPoint, line.EndPoint }), u);

    CopyProperties(l, line);
    l.length = line.Length;
    try
    {
      l.bbox = BoxToSpeckle(line.BoundingBox, u);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // TODO: Should a Line even have a bounding box?
    }
    return l;
  }

  /// <summary>
  /// SpeckleLine to DS Line
  /// </summary>
  /// <param name="line"></param>
  /// <returns></returns>
  public DS.Line LineToNative(Line line)
  {
    var ptStart = PointToNative(line.start);
    var ptEnd = PointToNative(line.end);

    var ln = DS.Line.ByStartPointEndPoint(ptStart, ptEnd);

    ptStart.Dispose();
    ptEnd.Dispose();

    return ln.SetDynamoProperties<DS.Line>(GetDynamicMembersFromBase(line));
  }

  /// <summary>
  /// DS Polygon to closed SpecklePolyline
  /// </summary>
  /// <param name="polygon"></param>
  /// <returns></returns>
  public Polyline PolylineToSpeckle(DS.Polygon polygon, string units = null)
  {
    var u = units ?? ModelUnits;
    var poly = new Polyline(PointListToFlatList(polygon.Points), u) { closed = true, };
    CopyProperties(poly, polygon);
    poly.length = polygon.Length;
    poly.bbox = BoxToSpeckle(polygon.BoundingBox, u);
    return poly;
  }

  /// <summary>
  /// DS Rectangle to SpecklePolyline
  /// </summary>
  /// <param name="rect"></param>
  /// <returns></returns>
  public Polyline PolylineToSpeckle(DS.Rectangle rectangle, string units = null)
  {
    var rect = PolylineToSpeckle(rectangle as DS.Polygon, units ?? ModelUnits);

    CopyProperties(rect, rectangle);
    return rect;
  }

  /// <summary>
  /// SpecklePolyline to DS Rectangle if closed , four points and sides parallel;
  /// DS Polygon if closed or DS Polycurve otherwise
  /// </summary>
  /// <param name="polyline"></param>
  /// <returns></returns>
  public DS.Curve PolylineToNative(Polyline polyline)
  {
    var points = ArrayToPointList(polyline.value, polyline.units);
    if (polyline.closed)
    {
      return DS
        .PolyCurve.ByPoints(points)
        .CloseWithLine()
        .SetDynamoProperties<DS.PolyCurve>(GetDynamicMembersFromBase(polyline));
    }

    return PolyCurve.ByPoints(points).SetDynamoProperties<PolyCurve>(GetDynamicMembersFromBase(polyline));
  }

  #endregion

  #region Curves

  /// <summary>
  /// DS Circle to SpeckleCircle.
  /// </summary>
  /// <param name="circ"></param>
  /// <returns></returns>
  public Circle CircleToSpeckle(DS.Circle circ, string units = null)
  {
    var u = units ?? ModelUnits;
    using (DS.Vector xAxis = DS.Vector.ByTwoPoints(circ.CenterPoint, circ.StartPoint))
    using (DS.Plane plane = DS.Plane.ByOriginNormalXAxis(circ.CenterPoint, circ.Normal, xAxis))
    {
      var myCircle = new Circle(PlaneToSpeckle(plane, u), circ.Radius, u);
      CopyProperties(myCircle, circ);
      myCircle.length = circ.Length;
      myCircle.bbox = BoxToSpeckle(circ.BoundingBox, u);
      return myCircle;
    }
  }

  /// <summary>
  /// SpeckleCircle to DS Circle. Rotating the circle is due to a bug in ProtoGeometry
  /// that will be solved on Dynamo 2.1.
  /// </summary>
  /// <param name="circ"></param>
  /// <returns></returns>
  public DS.Circle CircleToNative(Circle circ)
  {
    using (DS.Plane basePlane = PlaneToNative(circ.plane))
    using (DS.Circle preCircle = DS.Circle.ByPlaneRadius(basePlane, ScaleToNative(circ.radius.Value, circ.units)))
    using (DS.Vector preXvector = DS.Vector.ByTwoPoints(preCircle.CenterPoint, preCircle.StartPoint))
    {
      double angle = preXvector.AngleAboutAxis(basePlane.XAxis, basePlane.Normal);
      var circle = (DS.Circle)preCircle.Rotate(basePlane, angle);

      return circle.SetDynamoProperties<DS.Circle>(GetDynamicMembersFromBase(circ));
    }
  }

  /// <summary>
  /// DS Arc to SpeckleArc
  /// </summary>
  /// <param name="a"></param>
  /// <returns></returns>
  public Arc ArcToSpeckle(DS.Arc a, string units = null)
  {
    var u = units ?? ModelUnits;
    using (DS.Vector xAxis = DS.Vector.ByTwoPoints(a.CenterPoint, a.StartPoint))
    using (DS.Plane basePlane = DS.Plane.ByOriginNormalXAxis(a.CenterPoint, a.Normal, xAxis))
    using (DS.Point midPoint = a.PointAtParameter(0.5))
    {
      var arc = new Arc(
        PlaneToSpeckle(basePlane, u),
        a.Radius,
        0, // This becomes 0 as arcs are interpreted to start from the plane's X axis.
        a.SweepAngle.ToRadians(),
        a.SweepAngle.ToRadians(),
        u
      );
      arc.startPoint = PointToSpeckle(a.StartPoint);
      arc.midPoint = PointToSpeckle(midPoint);
      arc.endPoint = PointToSpeckle(a.EndPoint);

      CopyProperties(arc, a);

      arc.length = a.Length;
      arc.bbox = BoxToSpeckle(a.BoundingBox);

      return arc;
    }
  }

  /// <summary>
  /// SpeckleArc to DS Arc
  /// </summary>
  /// <param name="a"></param>
  /// <returns></returns>
  public DS.Arc ArcToNative(Arc a)
  {
    var arc = DS.Arc.ByThreePoints(PointToNative(a.startPoint), PointToNative(a.midPoint), PointToNative(a.endPoint));
    return arc.SetDynamoProperties<DS.Arc>(GetDynamicMembersFromBase(a));
  }

  /// <summary>
  /// DS Ellipse to SpeckleEllipse
  /// </summary>
  /// <param name="e"></param>
  /// <returns></returns>
  public Ellipse EllipseToSpeckle(DS.Ellipse e, string units = null)
  {
    var u = units ?? ModelUnits;
    using (DS.Plane basePlane = DS.Plane.ByOriginNormalXAxis(e.CenterPoint, e.Normal, e.MajorAxis))
    {
      var ellipse = new Ellipse(
        PlaneToSpeckle(basePlane, u),
        e.MajorAxis.Length,
        e.MinorAxis.Length,
        new Interval(e.StartParameter(), e.EndParameter()),
        null,
        u
      );

      CopyProperties(ellipse, e);

      ellipse.length = e.Length;
      ellipse.bbox = BoxToSpeckle(e.BoundingBox, u);

      return ellipse;
    }
  }

  /// <summary>
  /// SpeckleEllipse to DS Ellipse
  /// </summary>
  /// <param name="e"></param>
  /// <returns></returns>
  public DS.Curve EllipseToNative(Ellipse e)
  {
    if (e.trimDomain != null)
    {
      // Curve is an ellipse arc
      var ellipseArc = DS.EllipseArc.ByPlaneRadiiAngles(
        PlaneToNative(e.plane),
        ScaleToNative(e.firstRadius.Value, e.units),
        ScaleToNative(e.secondRadius.Value, e.units),
        e.trimDomain.start.Value * 360 / (2 * Math.PI),
        (double)(e.trimDomain.end - e.trimDomain.start) * 360 / (2 * Math.PI)
      );
      return ellipseArc;
    }
    else
    {
      // Curve is an ellipse
      var ellipse = DS.Ellipse.ByPlaneRadii(
        PlaneToNative(e.plane),
        ScaleToNative(e.firstRadius.Value, e.units),
        ScaleToNative(e.secondRadius.Value, e.units)
      );
      ellipse.SetDynamoProperties<DS.Ellipse>(GetDynamicMembersFromBase(e));
      return ellipse;
    }
  }

  /// <summary>
  /// DS EllipseArc to Speckle Ellipse
  /// </summary>
  /// <param name="arc"></param>
  /// <returns></returns>
  public Ellipse EllipseToSpeckle(EllipseArc arc, string units = null)
  {
    var u = units ?? ModelUnits;
    var ellipArc = new Ellipse(
      PlaneToSpeckle(arc.Plane, u),
      arc.MajorAxis.Length,
      arc.MinorAxis.Length,
      new Interval(0, 2 * Math.PI),
      new Interval(arc.StartAngle, arc.StartAngle + arc.SweepAngle),
      u
    );

    CopyProperties(ellipArc, arc);

    ellipArc.length = arc.Length;
    ellipArc.bbox = BoxToSpeckle(arc.BoundingBox, u);

    return ellipArc;
  }

  /// <summary>
  /// DS Polycurve to SpecklePolyline if all curves are linear
  /// SpecklePolycurve otherwise
  /// </summary>
  /// <param name="polycurve"></param>
  /// <returns name="speckleObject"></returns>
  public Base PolycurveToSpeckle(PolyCurve polycurve, string units = null)
  {
    var u = units ?? ModelUnits;
    if (polycurve.IsPolyline())
    {
      var points = polycurve.Curves().SelectMany(c => PointToArray(c.StartPoint)).ToList();
      points.AddRange(PointToArray(polycurve.Curves().Last().EndPoint));
      var poly = new Polyline(points, u);

      CopyProperties(poly, polycurve);
      poly.length = polycurve.Length;
      poly.bbox = BoxToSpeckle(polycurve.BoundingBox, u);

      return poly;
    }
    else
    {
      Polycurve spkPolycurve = new(u);
      CopyProperties(spkPolycurve, polycurve);

      spkPolycurve.segments = polycurve.Curves().Select(c => (ICurve)CurveToSpeckle(c, u)).ToList();

      spkPolycurve.length = polycurve.Length;
      spkPolycurve.bbox = BoxToSpeckle(polycurve.BoundingBox, u);

      return spkPolycurve;
    }
  }

  public PolyCurve PolycurveToNative(Polycurve polycurve)
  {
    DS.Curve[] curves = new DS.Curve[polycurve.segments.Count];
    for (var i = 0; i < polycurve.segments.Count; i++)
    {
      switch (polycurve.segments[i])
      {
        case Line curve:
          curves[i] = LineToNative(curve);
          break;
        case Arc curve:
          curves[i] = ArcToNative(curve);
          break;
        case Circle curve:
          curves[i] = CircleToNative(curve);
          break;
        case Ellipse curve:
          curves[i] = EllipseToNative(curve);
          break;
        case Spiral curve:
          curves[i] = PolylineToNative(curve.displayValue);
          break;
        case Polycurve curve:
          curves[i] = PolycurveToNative(curve);
          break;
        case Polyline curve:
          curves[i] = PolylineToNative(curve);
          break;
        case Curve curve:
          curves[i] = CurveToNative(curve);
          break;
      }
    }

    PolyCurve polyCrv = null;
    if (curves.Any())
    {
      polyCrv = PolyCurve.ByJoinedCurves(curves);
      polyCrv = polyCrv.SetDynamoProperties<PolyCurve>(GetDynamicMembersFromBase(polycurve));
    }

    return polyCrv;
  }

  public Base CurveToSpeckle(DS.Curve curve, string units = null)
  {
    var u = units ?? ModelUnits;
    Base speckleCurve;
    if (curve.IsLinear())
    {
      using (DS.Line line = curve.GetAsLine())
      {
        speckleCurve = LineToSpeckle(line, u);
      }
    }
    else if (curve.IsArc())
    {
      using (DS.Arc arc = curve.GetAsArc())
      {
        speckleCurve = ArcToSpeckle(arc, u);
      }
    }
    else if (curve.IsCircle())
    {
      using (DS.Circle circle = curve.GetAsCircle())
      {
        speckleCurve = CircleToSpeckle(circle, u);
      }
    }
    else if (curve.IsEllipse())
    {
      using (DS.Ellipse ellipse = curve.GetAsEllipse())
      {
        speckleCurve = EllipseToSpeckle(ellipse, u);
      }
    }
    else
    {
      speckleCurve = CurveToSpeckle(curve.ToNurbsCurve(), u);
    }

    CopyProperties(speckleCurve, curve);
    return speckleCurve;
  }

  public NurbsCurve CurveToNative(Curve curve)
  {
    var points = ArrayToPointList(curve.points, curve.units);
    var dsKnots = curve.knots;

    NurbsCurve nurbsCurve = NurbsCurve.ByControlPointsWeightsKnots(
      points,
      curve.weights.ToArray(),
      dsKnots.ToArray(),
      curve.degree
    );

    return nurbsCurve.SetDynamoProperties<NurbsCurve>(GetDynamicMembersFromBase(curve));
  }

  public Base CurveToSpeckle(NurbsCurve curve, string units = null)
  {
    var u = units ?? ModelUnits;
    Base speckleCurve;
    if (curve.IsLinear())
    {
      using (DS.Line line = curve.GetAsLine())
      {
        speckleCurve = LineToSpeckle(line, u);
      }
    }
    else if (curve.IsArc())
    {
      using (DS.Arc arc = curve.GetAsArc())
      {
        speckleCurve = ArcToSpeckle(arc, u);
      }
    }
    else if (curve.IsCircle())
    {
      using (DS.Circle circle = curve.GetAsCircle())
      {
        speckleCurve = CircleToSpeckle(circle, u);
      }
    }
    else if (curve.IsEllipse())
    {
      using (DS.Ellipse ellipse = curve.GetAsEllipse())
      {
        speckleCurve = EllipseToSpeckle(ellipse, u);
      }
    }
    else
    {
      // SpeckleCurve DisplayValue
      DS.Curve[] curves = curve.ApproximateWithArcAndLineSegments();
      List<double> polylineCoordinates = curves
        .SelectMany(c => PointListToFlatList(new DS.Point[2] { c.StartPoint, c.EndPoint }))
        .ToList();
      polylineCoordinates.AddRange(PointToArray(curves.Last().EndPoint));
      curves.ForEach(c => c.Dispose());

      Polyline displayValue = new(polylineCoordinates, u);
      List<double> dsKnots = curve.Knots().ToList();

      Curve spkCurve = new(displayValue, u);
      spkCurve.weights = curve.Weights().ToList();
      spkCurve.points = PointListToFlatList(curve.ControlPoints());
      spkCurve.knots = dsKnots;
      spkCurve.degree = curve.Degree;
      spkCurve.periodic = curve.IsPeriodic;
      spkCurve.rational = curve.IsRational;
      spkCurve.closed = curve.IsClosed;
      spkCurve.domain = new Interval(curve.StartParameter(), curve.EndParameter());
      spkCurve.length = curve.Length;
      spkCurve.bbox = BoxToSpeckle(curve.BoundingBox, u);

      speckleCurve = spkCurve;
    }

    CopyProperties(speckleCurve, curve);
    return speckleCurve;
  }

  // TODO: send this as a spiral class
  public Base HelixToSpeckle(Helix helix, string units = null)
  {
    var u = units ?? ModelUnits;

    /* untested code to send as spiral
    using (DS.Plane basePlane = DS.Plane.ByOriginNormal(helix.AxisPoint, helix.Normal))
    {
      var spiral = new Spiral();

      spiral.startPoint = PointToSpeckle(helix.StartPoint, u);
      spiral.endPoint = PointToSpeckle(helix.EndPoint, u);
      spiral.plane = PlaneToSpeckle(basePlane, u);
      spiral.pitchAxis = VectorToSpeckle(helix.AxisDirection, u);
      spiral.pitch = helix.Pitch;
      spiral.turns = helix.Angle / (2 * Math.PI);

      // display value
      DS.Curve[] curves = helix.ApproximateWithArcAndLineSegments();
      List<double> polylineCoordinates =
        curves.SelectMany(c => PointListToFlatList(new DS.Point[2] { c.StartPoint, c.EndPoint })).ToList();
      polylineCoordinates.AddRange(PointToArray(curves.Last().EndPoint));
      curves.ForEach(c => c.Dispose());
      Polyline displayValue = new Polyline(polylineCoordinates, u);
      spiral.displayValue = displayValue;

      CopyProperties(spiral, helix);

      spiral.length = helix.Length;
      spiral.bbox = BoxToSpeckle(helix.BoundingBox.ToCuboid(), u);

      return spiral;
    }
    */

    using (NurbsCurve nurbsCurve = helix.ToNurbsCurve())
    {
      var curve = CurveToSpeckle(nurbsCurve, u);
      CopyProperties(curve, helix);
      return curve;
    }
  }

  #endregion

  #region mesh

  public DS.Mesh BrepToNative(Brep brep)
  {
    if (brep.displayValue != null)
    {
      var meshToNative = MeshToNative(brep.displayValue[0]);
      return meshToNative;
    }

    return null;
  }

  // Meshes
  public Mesh MeshToSpeckle(DS.Mesh mesh, string units = null)
  {
    var u = units ?? ModelUnits;
    var vertices = PointListToFlatList(mesh.VertexPositions);
    var defaultColour = System.Drawing.Color.FromArgb(255, 100, 100, 100);

    var faces = mesh
      .FaceIndices.SelectMany(f =>
      {
        if (f.Count == 4)
        {
          return new int[] { 4, (int)f.A, (int)f.B, (int)f.C, (int)f.D };
        }
        else
        {
          return new int[] { 3, (int)f.A, (int)f.B, (int)f.C };
        }
      })
      .ToList();

    //var colors = Enumerable.Repeat(defaultColour.ToArgb(), mesh.VertexPositions.Length).ToList();
    //double[] textureCoords;

    //if (SpeckleRhinoConverter.AddMeshTextureCoordinates)
    //{
    //  textureCoords = mesh.TextureCoordinates.Select(pt => pt).ToFlatArray();
    //  return new SpeckleMesh(vertices, faces, Colors, textureCoords, properties: mesh.UserDictionary.ToSpeckle());
    //}

    var speckleMesh = new Mesh(vertices, faces, units: u);

    CopyProperties(speckleMesh, mesh);

    using (var box = ComputeMeshBoundingBox(mesh))
    {
      speckleMesh.bbox = BoxToSpeckle(box, u);
    }

    return speckleMesh;
  }

  private DS.Cuboid ComputeMeshBoundingBox(DS.Mesh mesh)
  {
    double lowX = double.PositiveInfinity,
      lowY = double.PositiveInfinity,
      lowZ = double.PositiveInfinity;
    double highX = double.NegativeInfinity,
      highY = double.NegativeInfinity,
      highZ = double.NegativeInfinity;
    mesh.VertexPositions.ForEach(pos =>
    {
      if (pos.X < lowX)
      {
        lowX = pos.X;
      }

      if (pos.Y < lowY)
      {
        lowY = pos.Y;
      }

      if (pos.Z < lowZ)
      {
        lowZ = pos.Z;
      }

      if (pos.X > highX)
      {
        highX = pos.X;
      }

      if (pos.Y > highY)
      {
        highY = pos.Y;
      }

      if (pos.Z > highZ)
      {
        highZ = pos.Z;
      }
    });

    using (var low = DS.Point.ByCoordinates(lowX, lowY, lowZ))
    using (var high = DS.Point.ByCoordinates(highX, highY, highZ))
    {
      return DS.Cuboid.ByCorners(low, high);
    }
  }

  public DS.Mesh MeshToNative(Mesh mesh)
  {
    // Triangulate the mesh's NGons, since they're not supported in Dynamo
    mesh.TriangulateMesh(true);

    var points = ArrayToPointList(mesh.vertices, mesh.units);
    var faces = new List<IndexGroup>();
    var i = 0;
    var faceIndices = new List<int>(mesh.faces);
    while (i < faceIndices.Count)
    {
      if (faceIndices[i] == 0 || faceIndices[i] == 3)
      {
        // triangle
        var ig = IndexGroup.ByIndices((uint)faceIndices[i + 1], (uint)faceIndices[i + 2], (uint)faceIndices[i + 3]);
        faces.Add(ig);
        i += 4;
      }
      else if (faceIndices[i] == 1 || faceIndices[i] == 4)
      {
        // quad
        var ig = IndexGroup.ByIndices(
          (uint)faceIndices[i + 1],
          (uint)faceIndices[i + 2],
          (uint)faceIndices[i + 3],
          (uint)faceIndices[i + 4]
        );
        faces.Add(ig);
        i += 5;
      }
      else
      {
        var fcount = faceIndices[i];
        i += fcount + 1;
      }
    }

    var dsMesh = DS.Mesh.ByPointsFaceIndices(points, faces);

    dsMesh.SetDynamoProperties<DS.Mesh>(GetDynamicMembersFromBase(mesh));

    return dsMesh;
  }

  #endregion

  public Cuboid BoxToNative(Box box)
  {
    using (var coordinateSystem = PlaneToNative(box.basePlane).ToCoordinateSystem())
    using (
      var cLow = DS.Point.ByCartesianCoordinates(
        coordinateSystem,
        box.xSize.start ?? 0,
        box.ySize.start ?? 0,
        box.zSize.start ?? 0
      )
    )
    using (
      var cHigh = DS.Point.ByCartesianCoordinates(
        coordinateSystem,
        box.xSize.end ?? 0,
        box.ySize.end ?? 0,
        box.zSize.end ?? 0
      )
    )
    {
      return Cuboid.ByCorners(cLow, cHigh);
    }
  }

  public Box BoxToSpeckle(BoundingBox box, string units = null)
  {
    var u = units ?? ModelUnits;
    return new Box(
      PlaneToSpeckle(box.ContextCoordinateSystem.XYPlane),
      new Interval(box.MinPoint.X, box.MaxPoint.X),
      new Interval(box.MinPoint.Y, box.MaxPoint.Y),
      new Interval(box.MinPoint.Z, box.MaxPoint.Z),
      u
    );
  }

  public Box BoxToSpeckle(Cuboid box, string units = null)
  {
    var u = units ?? ModelUnits;
    var plane = PlaneToSpeckle(box.ContextCoordinateSystem.XYPlane, u);

    // Todo: Check for cubes that are offset from the plane origin to ensure correct positioning.
    var boxToSpeckle = new Box(
      plane,
      new Interval(-box.Width / 2, box.Width / 2),
      new Interval(-box.Length / 2, box.Length / 2),
      new Interval(-box.Height / 2, box.Height / 2),
      u
    );
    boxToSpeckle.volume = box.Volume;
    boxToSpeckle.area = box.Area;
    return boxToSpeckle;
  }

  public NurbsSurface SurfaceToNative(Surface surface)
  {
    var points = new DS.Point[][] { };
    var weights = new double[][] { };

    var controlPoints = surface.GetControlPoints();

    points = controlPoints
      .Select(row =>
        row.Select(p =>
            DS.Point.ByCoordinates(
              ScaleToNative(p.x, p.units),
              ScaleToNative(p.y, p.units),
              ScaleToNative(p.z, p.units)
            )
          )
          .ToArray()
      )
      .ToArray();

    weights = controlPoints.Select(row => row.Select(p => p.weight).ToArray()).ToArray();

    var knotsU = surface.knotsU;
    var knotsV = surface.knotsV;

    var result = DS.NurbsSurface.ByControlPointsWeightsKnots(
      points,
      weights,
      knotsU.ToArray(),
      surface.knotsV.ToArray(),
      surface.degreeU,
      surface.degreeV
    );
    return result;
  }

  public Surface SurfaceToSpeckle(NurbsSurface surface, string units = null)
  {
    var u = units ?? ModelUnits;
    var result = new Surface();
    result.units = u;
    // Set control points
    var dsPoints = surface.ControlPoints();
    var dsWeights = surface.Weights();
    var points = new List<List<ControlPoint>>();
    for (var i = 0; i < dsPoints.Length; i++)
    {
      var row = new List<ControlPoint>();
      for (var j = 0; j < dsPoints[i].Length; j++)
      {
        var dsPoint = dsPoints[i][j];
        var dsWeight = dsWeights[i][j];
        row.Add(new ControlPoint(dsPoint.X, dsPoint.Y, dsPoint.Z, dsWeight, null));
      }

      points.Add(row);
    }

    result.SetControlPoints(points);

    // Set degree
    result.degreeU = surface.DegreeU;
    result.degreeV = surface.DegreeV;

    // Set knot vectors
    result.knotsU = surface.UKnots().ToList();
    result.knotsV = surface.VKnots().ToList();

    // Set other
    result.rational = surface.IsRational;
    result.closedU = surface.ClosedInU;
    result.closedV = surface.ClosedInV;

    result.area = surface.Area;
    result.bbox = BoxToSpeckle(surface.BoundingBox, u);

    return result;
  }

  /// <summary>
  /// Copies props from a design script entity to a speckle object.
  /// </summary>
  /// <param name="to"></param>
  /// <param name="from"></param>
  public void CopyProperties(Base to, DesignScriptEntity from)
  {
    var dict = from.GetSpeckleProperties();
    foreach (var kvp in dict)
    {
      to[kvp.Key] = kvp.Value;
    }
  }

  public Dictionary<string, object> GetDynamicMembersFromBase(Base obj)
  {
    return obj.GetMembers(DynamicBaseMemberType.Dynamic);
  }
}

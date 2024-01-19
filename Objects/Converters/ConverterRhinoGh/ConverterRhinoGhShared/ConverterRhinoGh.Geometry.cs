#if GRASSHOPPER
using Grasshopper.Kernel.Types;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using Objects.Geometry;
using Objects.Other;
using Objects.Primitive;
using Objects.Utils;
using Rhino.Geometry.Collections;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Point = Objects.Geometry.Point;
using RH = Rhino.Geometry;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // Convenience methods point:
  public double[] PointToArray(RH.Point3d pt)
  {
    return new[] { pt.X, pt.Y, pt.Z };
  }

  /// Mass point converter
  /// <remarks>This is faster than calling <see cref="Mesh.GetPoints"/> <see cref="Objects.Geometry.Polyline.GetPoints"/></remarks>
  public List<RH.Point3d> PointListToNative(IList<double> arr, string units)
  {
    if (arr.Count % 3 != 0)
    {
      throw new SpeckleException("Array malformed: length%3 != 0.");
    }

    var points = new List<RH.Point3d>(arr.Count / 3);

    var sf = Units.GetConversionFactor(units, ModelUnits);
    for (int i = 2; i < arr.Count; i += 3)
    {
      points.Add(new RH.Point3d(arr[i - 2] * sf, arr[i - 1] * sf, arr[i] * sf));
    }

    return points;
  }

  public List<double> PointsToFlatList(IEnumerable<RH.Point3d> points)
  {
    return points.SelectMany(PointToArray).ToList();
  }

  // Points
  // GhCapture?
  public Point PointToSpeckle(RH.Point3d pt, string units = null)
  {
    return new Point(pt.X, pt.Y, pt.Z, units ?? ModelUnits);
  }

  // Rh Capture?
  public RH.Point PointToNative(Point pt)
  {
    double scaleFactor = ScaleToNative(1, pt.units);
    var myPoint = new RH.Point(new RH.Point3d(pt.x * scaleFactor, pt.y * scaleFactor, pt.z * scaleFactor));

    return myPoint;
  }

  public Point PointToSpeckle(RH.Point pt, string units = null)
  {
    return new Point(pt.Location.X, pt.Location.Y, pt.Location.Z, units ?? ModelUnits);
  }

  // Vectors
  public Vector VectorToSpeckle(RH.Vector3d pt, string units = null)
  {
    return new Vector(pt.X, pt.Y, pt.Z, units ?? ModelUnits);
  }

  public RH.Vector3d VectorToNative(Vector pt)
  {
    return new RH.Vector3d(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units));
  }

  // Interval
  public Interval IntervalToSpeckle(RH.Interval interval)
  {
    var speckleInterval = new Interval(interval.T0, interval.T1);
    return speckleInterval;
  }

  public RH.Interval IntervalToNative(Interval interval, string units = Units.None)
  {
    return new RH.Interval(ScaleToNative((double)interval.start, units), ScaleToNative((double)interval.end, units));
  }

  // Plane
  public Plane PlaneToSpeckle(RH.Plane plane, string units = null)
  {
    var u = units ?? ModelUnits;
    return new Plane(
      PointToSpeckle(plane.Origin, u),
      VectorToSpeckle(plane.Normal, u),
      VectorToSpeckle(plane.XAxis, u),
      VectorToSpeckle(plane.YAxis, u),
      u
    );
  }

  public RH.Plane PlaneToNative(Plane plane)
  {
    var xAxis = VectorToNative(plane.xdir);
    xAxis.Unitize();
    var yAxis = VectorToNative(plane.ydir);
    yAxis.Unitize();

    return new RH.Plane(PointToNative(plane.origin).Location, xAxis, yAxis);
  }

  // Line
  // Gh Line capture
  public Line LineToSpeckle(RH.Line line, string units = null)
  {
    var u = units ?? ModelUnits;
    var sLine = new Line(PointToSpeckle(line.From, u), PointToSpeckle(line.To, u), u);
    sLine.length = line.Length;
    sLine.domain = new Interval(0, line.Length);
    var box = new RH.Box(line.BoundingBox);
    sLine.bbox = BoxToSpeckle(box, u);
    return sLine;
  }

  // Rh Line capture
  public Line LineToSpeckle(RH.LineCurve line, string units = null)
  {
    var u = units ?? ModelUnits;
    var sLine = new Line(PointToSpeckle(line.PointAtStart, u), PointToSpeckle(line.PointAtEnd, u), u)
    {
      domain = IntervalToSpeckle(line.Domain)
    };
    sLine.length = line.GetLength();
    var box = new RH.Box(line.GetBoundingBox(true));
    sLine.bbox = BoxToSpeckle(box, u);

    return sLine;
  }

  // Back again only to LINECURVES because we hate grasshopper and its dealings with rhinocommon
  public RH.LineCurve LineToNative(Line line)
  {
    var myLine = new RH.LineCurve(PointToNative(line.start).Location, PointToNative(line.end).Location);
    myLine.Domain = line.domain == null ? new RH.Interval(0, line.length) : IntervalToNative(line.domain);
    return myLine;
  }

  // Rectangles now and forever forward will become polylines
  public Polyline PolylineToSpeckle(RH.Rectangle3d rect, string units = null)
  {
    var u = units ?? ModelUnits;
    var length = rect.Height * 2 + rect.Width * 2;
    var sPoly = new Polyline(
      PointsToFlatList(new[] { rect.Corner(0), rect.Corner(1), rect.Corner(2), rect.Corner(3) }),
      u
    )
    {
      closed = true,
      area = rect.Area,
      bbox = BoxToSpeckle(new RH.Box(rect.BoundingBox), u),
      length = length,
      domain = new Interval(0, length)
    };

    return sPoly;
  }

  // Circle
  // Gh Capture
  public Circle CircleToSpeckle(RH.Circle circ, string units = null)
  {
    var u = units ?? ModelUnits;
    var circle = new Circle(PlaneToSpeckle(circ.Plane, u), circ.Radius, u);
    circle.domain = new Interval(0, 1);
    circle.length = 2 * Math.PI * circ.Radius;
    circle.area = Math.PI * circ.Radius * circ.Radius;
    return circle;
  }

  public RH.ArcCurve CircleToNative(Circle circ)
  {
    RH.Circle circle = new(PlaneToNative(circ.plane), ScaleToNative((double)circ.radius, circ.units));

    var myCircle = new RH.ArcCurve(circle);
    if (circ.domain != null)
    {
      myCircle.Domain = IntervalToNative(circ.domain);
    }

    return myCircle;
  }

  // Arc
  // Rh Capture can be a circle OR an arc
  public Base ArcToSpeckle(RH.ArcCurve a, string units = null)
  {
    var u = units ?? ModelUnits;
    if (a.IsClosed)
    {
      RH.Circle preCircle;
      a.TryGetCircle(out preCircle);
      Circle myCircle = CircleToSpeckle(preCircle, u);
      myCircle.domain = IntervalToSpeckle(a.Domain);
      myCircle.length = a.GetLength();
      myCircle.bbox = BoxToSpeckle(new RH.Box(a.GetBoundingBox(true)), u);
      return myCircle;
    }

    RH.Arc preArc;
    a.TryGetArc(out preArc);
    Arc myArc = ArcToSpeckle(preArc, u);
    myArc.domain = IntervalToSpeckle(a.Domain);
    myArc.length = a.GetLength();
    myArc.bbox = BoxToSpeckle(new RH.Box(a.GetBoundingBox(true)), u);
    return myArc;
  }

  // Gh Capture
  public Arc ArcToSpeckle(RH.Arc a, string units = null)
  {
    var u = units ?? ModelUnits;
    Arc arc = new(PlaneToSpeckle(a.Plane, u), a.Radius, a.StartAngle, a.EndAngle, a.Angle, u);
    arc.endPoint = PointToSpeckle(a.EndPoint, u);
    arc.startPoint = PointToSpeckle(a.StartPoint, u);
    arc.midPoint = PointToSpeckle(a.MidPoint, u);
    arc.domain = new Interval(0, 1);
    arc.length = a.Length;
    arc.bbox = BoxToSpeckle(new RH.Box(a.BoundingBox()), u);
    return arc;
  }

  public RH.ArcCurve ArcToNative(Arc arc)
  {
    var _arc = new RH.Arc(
      PointToNative(arc.startPoint).Location,
      PointToNative(arc.midPoint).Location,
      PointToNative(arc.endPoint).Location
    );

    var arcCurve = new RH.ArcCurve(_arc);
    if (arc.domain != null)
    {
      arcCurve.Domain = IntervalToNative(arc.domain);
    }

    return arcCurve;
  }

  //Ellipse
  // TODO: handle conversions that define Radius1/Radius2 as major/minor instead of xaxis/yaxis
  public Ellipse EllipseToSpeckle(RH.Ellipse e, string units = null)
  {
    var u = units ?? ModelUnits;
    var el = new Ellipse(PlaneToSpeckle(e.Plane, u), e.Radius1, e.Radius2, u);
    el.domain = new Interval(0, 1);
    el.length = e.ToNurbsCurve().GetLength();
    el.bbox = BoxToSpeckle(new RH.Box(e.ToNurbsCurve().GetBoundingBox(true)), u);
    el.area = Math.PI * e.Radius1 * e.Radius2; // Manual area computing, could not find the Rhino way...
    return el;
  }

  public RH.Curve EllipseToNative(Ellipse e)
  {
    RH.Ellipse elp =
      new(
        PlaneToNative(e.plane),
        ScaleToNative((double)e.firstRadius, e.units),
        ScaleToNative((double)e.secondRadius, e.units)
      );
    var myEllp = elp.ToNurbsCurve();

    if (e.domain != null)
    {
      myEllp.Domain = IntervalToNative(e.domain);
    }

    if (e.trimDomain != null)
    {
      myEllp = myEllp.Trim(IntervalToNative(e.trimDomain)).ToNurbsCurve();
    }

    return myEllp;
  }

  // Spiral
  public RH.Curve SpiralToNative(Spiral s)
  {
    /* Using display value polyline for now
    var axisStart = PointToNative(s.plane.origin).Location;
    var axisDir = VectorToNative(s.plane.normal);
    var radiusPoint = PointToNative(s.startPoint).Location;
    var pitch = ScaleToNative(s.pitch, s.units);
    var endPoint = PointToNative(s.endPoint).Location;
    var r1 = axisStart.DistanceTo(radiusPoint);
    double r2 = 0;
    if (pitch == 0)
      r2 = axisStart.DistanceTo(endPoint);
    var nurbs = NurbsCurve.CreateSpiral(axisStart, axisDir, radiusPoint, pitch, s.turns, r1, r2);
    if (nurbs != null && nurbs.IsValid)
      if (nurbs.SetEndPoint(endPoint)) // try to adjust endpoint to match exactly the spiral endpoint
        return nurbs;
    */
    return PolylineToNative(s.displayValue);
  }

  // Polyline
  // Gh Capture
  public ICurve PolylineToSpeckle(RH.Polyline poly, string units = null)
  {
    return PolylineToSpeckle(poly, null, units);
  }

  public ICurve PolylineToSpeckle(RH.Polyline poly, Interval domain, string units = null)
  {
    var u = units ?? ModelUnits;

    if (poly.Count == 2)
    {
      var l = LineToSpeckle(new RH.Line(poly[0], poly[1]), u);
      if (domain != null)
      {
        l.domain = domain;
      }

      return l;
    }

    var myPoly = new Polyline(PointsToFlatList(poly), u);
    myPoly.closed = poly.IsClosed;

    if (myPoly.closed)
    {
      myPoly.value.RemoveRange(myPoly.value.Count - 3, 3);
    }

    myPoly.domain = domain;
    myPoly.bbox = BoxToSpeckle(new RH.Box(poly.BoundingBox), u);
    myPoly.length = poly.Length;

    // TODO: Area of 3d polyline cannot be resolved...
    return myPoly;
  }

  // Rh Capture
  public Base PolylineToSpeckle(RH.PolylineCurve poly, string units = null)
  {
    var u = units ?? ModelUnits;
    RH.Polyline polyline;

    if (poly.TryGetPolyline(out polyline))
    {
      var intervalToSpeckle = IntervalToSpeckle(poly.Domain);
      if (polyline.Count == 2)
      {
        var polylineToSpeckle = new Line(PointsToFlatList(polyline), u) { domain = intervalToSpeckle };
        polylineToSpeckle.length = polyline.Length;
        var box = new RH.Box(poly.GetBoundingBox(true));
        polylineToSpeckle.bbox = BoxToSpeckle(box, u);
        return polylineToSpeckle;
      }

      var myPoly = new Polyline(PointsToFlatList(polyline), u);
      myPoly.closed = polyline.IsClosed;

      if (myPoly.closed)
      {
        myPoly.value.RemoveRange(myPoly.value.Count - 3, 3);
      }

      myPoly.domain = intervalToSpeckle;
      myPoly.bbox = BoxToSpeckle(new RH.Box(poly.GetBoundingBox(true)), u);
      myPoly.length = poly.GetLength();
      return myPoly;
    }

    return null;
  }

  // Deserialise
  public RH.PolylineCurve PolylineToNative(Polyline poly)
  {
    List<RH.Point3d> points = PointListToNative(poly.value, poly.units);

    if (poly.closed)
    {
      points.Add(points[0]);
    }

    var myPoly = new RH.PolylineCurve(points);
    if (poly.domain != null)
    {
      myPoly.Domain = IntervalToNative(poly.domain);
    }

    return myPoly;
  }

  // Polycurve
  // Rh Capture/Gh Capture
  public Polycurve PolycurveToSpeckle(RH.PolyCurve p, string units = null)
  {
    var u = units ?? ModelUnits;
    var myPoly = new Polycurve();
    myPoly.closed = p.IsClosed;
    myPoly.domain = IntervalToSpeckle(p.Domain);
    myPoly.length = p.GetLength();
    myPoly.bbox = BoxToSpeckle(new RH.Box(p.GetBoundingBox(true)), u);

    var segments = new List<RH.Curve>();
    CurveSegments(segments, p, true);

    //let the converter pick the best type of curve
    myPoly.segments = segments.Select(s => CurveToSpeckle(s, u)).ToList();
    myPoly.units = u;
    return myPoly;
  }

  public RH.PolyCurve PolycurveToNative(Polycurve p)
  {
    RH.PolyCurve myPolyc = new();
    var notes = new List<string>();

    foreach (var segment in p.segments)
    {
      try
      {
        //let the converter pick the best type of curve
        myPolyc.Append((RH.Curve)ConvertToNative((Base)segment));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        notes.Add($"Could not append curve {segment.GetType()} to PolyCurve");
      }
    }

    if (p.domain != null)
    {
      myPolyc.Domain = IntervalToNative(p.domain);
    }

    return myPolyc;
  }

  // Curve
  public RH.Curve CurveToNative(ICurve curve)
  {
    switch (curve)
    {
      case Circle circle:
        return CircleToNative(circle);

      case Arc arc:
        return ArcToNative(arc);

      case Ellipse ellipse:
        return EllipseToNative(ellipse);

      case Spiral spiral:
        return SpiralToNative(spiral);

      case Curve crv:
        return NurbsToNative(crv);

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

  public ICurve CurveToSpeckle(RH.Curve curve, string units = null)
  {
    var u = units ?? ModelUnits;
    var tolerance = Doc.ModelAbsoluteTolerance;
    RH.Plane pln = RH.Plane.Unset;
    curve.TryGetPlane(out pln, tolerance);

    if (curve is RH.PolyCurve polyCurve)
    {
      return PolycurveToSpeckle(polyCurve, u);
    }

    if (curve.IsCircle(tolerance) && curve.IsClosed)
    {
      if (curve.TryGetCircle(out var getObj, tolerance))
      {
        var cir = CircleToSpeckle(getObj, u);
        cir.domain = IntervalToSpeckle(curve.Domain);
        return cir;
      }
    }

    if (curve.IsArc(tolerance))
    {
      if (curve.TryGetArc(out var getObj, tolerance))
      {
        var arc = ArcToSpeckle(getObj, u);
        arc.domain = IntervalToSpeckle(curve.Domain);
        return arc;
      }
    }

    if (curve.IsEllipse(tolerance) && curve.IsClosed)
    {
      if (curve.TryGetEllipse(pln, out var getObj, tolerance))
      {
        var ellipse = EllipseToSpeckle(getObj, u);
        ellipse.domain = IntervalToSpeckle(curve.Domain);
        return ellipse;
      }
    }

    if (curve.IsLinear(tolerance) || curve.IsPolyline()) // defaults to polyline
    {
      if (curve.TryGetPolyline(out var getObj))
      {
        var polyline = PolylineToSpeckle(getObj, IntervalToSpeckle(curve.Domain), u);
        return polyline;
      }
    }

    return NurbsToSpeckle(curve.ToNurbsCurve(), u);
  }

  public Curve NurbsToSpeckle(RH.NurbsCurve curve, string units = null)
  {
    var u = units ?? ModelUnits;
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
      displayValue = PolylineToSpeckle(poly, u) as Polyline;
    }

    var myCurve = new Curve(displayValue, u);
    var nurbsCurve = curve.ToNurbsCurve();

    // increase knot multiplicity to (# control points + degree + 1)
    // add extra knots at start & end  because Rhino's knot multiplicity standard is (# control points + degree - 1)
    var knots = nurbsCurve.Knots.ToList();
    knots.Insert(0, knots[0]);
    knots.Insert(knots.Count - 1, knots[knots.Count - 1]);

    // Hack: Rebuild curve to prevent interior knot multiplicities.
    //var max = Math.Min(nurbsCurve.Points.Count-1, 3);
    //nurbsCurve = nurbsCurve.Rebuild(nurbsCurve.Points.Count, max, true);

    myCurve.weights = nurbsCurve.Points.Select(ctp => ctp.Weight).ToList();
    myCurve.points = PointsToFlatList(nurbsCurve.Points.Select(ctp => ctp.Location));
    myCurve.knots = knots;
    myCurve.degree = nurbsCurve.Degree;
    myCurve.periodic = nurbsCurve.IsPeriodic;
    myCurve.rational = nurbsCurve.IsRational;
    myCurve.domain = IntervalToSpeckle(nurbsCurve.Domain);
    myCurve.closed = nurbsCurve.IsClosed;
    myCurve.length = nurbsCurve.GetLength();
    myCurve.bbox = BoxToSpeckle(new RH.Box(nurbsCurve.GetBoundingBox(true)), u);

    return myCurve;
  }

  public RH.NurbsCurve NurbsToNative(Curve curve)
  {
    var ptsList = curve.GetPoints().Select(o => PointToNative(o).Location).ToList();

    var nurbsCurve = RH.NurbsCurve.Create(false, curve.degree, ptsList);
    if (nurbsCurve == null)
    {
      return null;
    }

    for (int j = 0; j < nurbsCurve.Points.Count; j++)
    {
      nurbsCurve.Points.SetPoint(j, ptsList[j], curve.weights[j]);
    }

    // check knot multiplicity to match Rhino's standard of (# control points + degree - 1)
    // skip extra knots at start & end if knot multiplicity is (# control points + degree + 1)
    int extraKnots = curve.knots.Count - nurbsCurve.Knots.Count;
    for (int j = 0; j < nurbsCurve.Knots.Count; j++)
    {
      if (extraKnots == 2)
      {
        nurbsCurve.Knots[j] = curve.knots[j + 1];
      }
      else
      {
        nurbsCurve.Knots[j] = curve.knots[j];
      }
    }

    nurbsCurve.Domain = IntervalToNative(curve.domain ?? new Interval(0, 1));
    return nurbsCurve;
  }

  // Box
  public Box BoxToSpeckle(RH.Box box, string units = null)
  {
    var u = units ?? ModelUnits;
    var speckleBox = new Box(
      PlaneToSpeckle(box.Plane, u),
      IntervalToSpeckle(box.X),
      IntervalToSpeckle(box.Y),
      IntervalToSpeckle(box.Z),
      u
    );
    speckleBox.area = box.Area;
    speckleBox.volume = box.Volume;

    return speckleBox;
  }

  public RH.Box BoxToNative(Box box)
  {
    return new RH.Box(
      PlaneToNative(box.basePlane),
      IntervalToNative(box.xSize, box.units),
      IntervalToNative(box.ySize, box.units),
      IntervalToNative(box.zSize, box.units)
    );
  }

  // Meshes
  public Mesh MeshToSpeckle(RH.Mesh mesh, string units = null)
  {
    if (mesh.Vertices.Count == 0 || mesh.Faces.Count == 0)
    {
      Report.ConversionErrors.Add(new Exception("Cannot convert empty mesh (0 faces, 0 vertices)"));
      return null;
    }
    var u = units ?? ModelUnits;
    var verts = PointsToFlatList(mesh.Vertices.ToPoint3dArray());

    var faces = new List<int>();
    foreach (RH.MeshNgon polygon in mesh.GetNgonAndFacesEnumerable())
    {
      var vertIndices = polygon.BoundaryVertexIndexList();
      int n = vertIndices.Length;
      faces.Add(n);
      faces.AddRange(vertIndices.Select(vertIndex => (int)vertIndex));
    }

    var textureCoordinates = new List<double>(mesh.TextureCoordinates.Count * 2);
    foreach (var texCoord in mesh.TextureCoordinates)
    {
      textureCoordinates.Add(texCoord.X);
      textureCoordinates.Add(texCoord.Y);
    }

    var colors = mesh.VertexColors.Select(cl => cl.ToArgb()).ToList();

    var speckleMesh = new Mesh(verts, faces, colors, textureCoordinates, u)
    {
      volume = mesh.IsClosed ? mesh.Volume() : 0,
      bbox = BoxToSpeckle(new RH.Box(mesh.GetBoundingBox(true)), u)
    };

    return speckleMesh;
  }

#if RHINO7_OR_GREATER
  public Mesh MeshToSpeckle(RH.SubD mesh, string units = null)
    {
      var u = units ?? ModelUnits;

      var vertices = new List<RH.Point3d>();
      var subDVertices = new List<RH.SubDVertex>();
      for(int i = 0 ; i < mesh.Vertices.Count; i++)
      {
        vertices.Add(mesh.Vertices.Find(i).ControlNetPoint);
        subDVertices.Add(mesh.Vertices.Find(i));
      }
      var verts = PointsToFlatList(vertices);

      var Faces = mesh.Faces.SelectMany(face =>
      {
        if (face.VertexCount == 4)
        {
          return new[] { 4, subDVertices.IndexOf(face.VertexAt(0)), subDVertices.IndexOf(face.VertexAt(1)), subDVertices.IndexOf(face.VertexAt(2)), subDVertices.IndexOf(face.VertexAt(3)) };
        }

        return new[] { 3, subDVertices.IndexOf(face.VertexAt(0)), subDVertices.IndexOf(face.VertexAt(1)), subDVertices.IndexOf(face.VertexAt(2)) };
      }).ToList();

      var speckleMesh = new Mesh(verts, Faces, null, null, u);
      speckleMesh.bbox = BoxToSpeckle(new RH.Box(mesh.GetBoundingBox(true)), u);

      return speckleMesh;
    }
#endif

  public RH.Mesh MeshToNative(Mesh mesh)
  {
    mesh.AlignVerticesWithTexCoordsByIndex();

    RH.Mesh m = new();
    m.Vertices.AddVertices(PointListToNative(mesh.vertices, mesh.units));
    m.VertexColors.SetColors(mesh.colors.Select(Color.FromArgb).ToArray());

    var textureCoordinates = new RH.Point2f[mesh.TextureCoordinatesCount];
    for (int ti = 0; ti < mesh.TextureCoordinatesCount; ti++)
    {
      var (u, v) = mesh.GetTextureCoordinate(ti);
      textureCoordinates[ti] = new RH.Point2f(u, v);
    }
    m.TextureCoordinates.SetTextureCoordinates(textureCoordinates);

    int i = 0;
    while (i < mesh.faces.Count)
    {
      int n = mesh.faces[i];
      if (n < 3)
      {
        n += 3; // 0 -> 3, 1 -> 4
      }

      if (n == 3)
      {
        // triangle
        m.Faces.AddFace(new RH.MeshFace(mesh.faces[i + 1], mesh.faces[i + 2], mesh.faces[i + 3]));
      }
      else if (n == 4)
      {
        // quad
        m.Faces.AddFace(new RH.MeshFace(mesh.faces[i + 1], mesh.faces[i + 2], mesh.faces[i + 3], mesh.faces[i + 4]));
      }
      else
      {
        // n-gon
        var triangles = MeshTriangulationHelper.TriangulateFace(i, mesh, false);

        var faceIndices = new List<int>(triangles.Count);
        for (int t = 0; t < triangles.Count; t += 3)
        {
          var face = new RH.MeshFace(triangles[t], triangles[t + 1], triangles[t + 2]);
          faceIndices.Add(m.Faces.AddFace(face));
        }

        RH.MeshNgon ngon = RH.MeshNgon.Create(mesh.faces.GetRange(i + 1, n), faceIndices);
        m.Ngons.AddNgon(ngon);
      }

      i += n + 1;
    }
    m.Faces.CullDegenerateFaces();

#if RHINO7_OR_GREATER
    // get receive mesh setting
    var meshSetting = Settings.ContainsKey("receive-mesh")
      ? Settings["receive-mesh"]
      : string.Empty;

    if (meshSetting == "Merge Coplanar Faces")
    {
      m.MergeAllCoplanarFaces(Doc.ModelAbsoluteTolerance, Doc.ModelAngleToleranceRadians);
    }
#endif

    return m;
  }

  // Pointcloud
  public Pointcloud PointcloudToSpeckle(RH.PointCloud pointcloud, string units = null)
  {
    var u = units ?? ModelUnits;

    var _pointcloud = new Pointcloud
    {
      points = PointsToFlatList(pointcloud.GetPoints()),
      colors = pointcloud.GetColors().Select(o => o.ToArgb()).ToList(),
      bbox = BoxToSpeckle(new RH.Box(pointcloud.GetBoundingBox(true)), u),
      units = u
    };

    return _pointcloud;
  }

  public RH.PointCloud PointcloudToNative(Pointcloud pointcloud)
  {
    int numPoints = pointcloud.points.Count / 3;
    List<double> sPoints = pointcloud.points;
    RH.Point3d[] rhPoints = new RH.Point3d[numPoints];
    double scaleFactor = ScaleToNative(1, pointcloud.units);

    for (int i = 0; i < numPoints; i++)
    {
      rhPoints[i] = new RH.Point3d(
        sPoints[3 * i] * scaleFactor,
        sPoints[3 * i + 1] * scaleFactor,
        sPoints[3 * i + 2] * scaleFactor
      );
    }

    var _pointcloud = new RH.PointCloud(rhPoints);

    if (pointcloud.colors.Count == rhPoints.Length)
    {
      for (int i = 0; i < rhPoints.Length; i++)
      {
        _pointcloud[i].Color = Color.FromArgb(pointcloud.colors[i]);
      }
    }

    return _pointcloud;
  }

  private void AttachPointcloudParams(Base specklePointcloud, RH.PointCloud pointcloud)
  {
    // normals
    if (pointcloud.ContainsNormals)
    {
      var normals = pointcloud.GetNormals().Select(o => VectorToSpeckle(o, ModelUnits)).ToList();
      specklePointcloud["normals"] = normals;
    }
  }

  private RH.PointCloud SetPointcloudParams(RH.PointCloud pointcloud, Base specklePointcloud)
  {
    // normals
    var normals = specklePointcloud["normals"] as List<Vector>;
    if (normals != null && normals.Count == pointcloud.Count)
    {
      for (int i = 0; i < pointcloud.Count; i++)
      {
        pointcloud[i].Normal = VectorToNative(normals[i]);
      }
    }

    return pointcloud;
  }

  /// <summary>
  /// Converts a Rhino <see cref="Rhino.Geometry.Brep"/> instance to a Speckle <see cref="Brep"/>
  /// </summary>
  /// <param name="brep">BREP to be converted.</param>
  /// <returns></returns>
  public Brep BrepToSpeckle(RH.Brep brep, string units = null, RH.Mesh previewMesh = null, RenderMaterial mat = null)
  {
    var tol = Doc.ModelAbsoluteTolerance;
    //tol = 0;
    var u = units ?? ModelUnits;
    brep.Repair(tol);

    if (PreprocessGeometry)
    {
      brep = BrepEncoder.ToRawBrep(brep, 1.0, Doc.ModelAngleToleranceRadians, Doc.ModelRelativeTolerance);
    }

    // get display mesh and attach render material to it if it exists
    var displayMesh = previewMesh ?? GetBrepDisplayMesh(brep);
    var displayValue = MeshToSpeckle(displayMesh, u);
    if (displayValue != null && mat != null)
    {
      displayValue["renderMaterial"] = mat;
    }

    var spcklBrep = new Brep(displayValue: displayValue, provenance: RhinoAppName, units: u);

    // Vertices, uv curves, 3d curves and surfaces
    spcklBrep.Vertices = brep.Vertices.Select(vertex => PointToSpeckle(vertex, u)).ToList();
    spcklBrep.Curve3D = brep.Curves3D.Select(curve3d => ConvertToSpeckle(curve3d) as ICurve).ToList();
    spcklBrep.Curve2D = brep.Curves2D.Select(c => CurveToSpeckle(c, Units.None)).ToList();
    spcklBrep.Surfaces = brep.Surfaces.Select(srf => SurfaceToSpeckle(srf.ToNurbsSurface(), u)).ToList();

    spcklBrep.IsClosed = brep.IsSolid;
    spcklBrep.Orientation = (BrepOrientation)brep.SolidOrientation;

    // Faces
    spcklBrep.Faces = brep.Faces
      .Select(
        f =>
          new BrepFace(
            spcklBrep,
            f.SurfaceIndex,
            f.Loops.Select(l => l.LoopIndex).ToList(),
            f.OuterLoop.LoopIndex,
            f.OrientationIsReversed
          )
      )
      .ToList();

    // Edges
    spcklBrep.Edges = brep.Edges
      .Select(
        edge =>
          new BrepEdge(
            spcklBrep,
            edge.EdgeCurveIndex,
            edge.TrimIndices(),
            edge.StartVertex?.VertexIndex ?? -1,
            edge.EndVertex?.VertexIndex ?? -1,
            edge.ProxyCurveIsReversed,
            IntervalToSpeckle(edge.Domain)
          )
      )
      .ToList();

    // Loops
    spcklBrep.Loops = brep.Loops
      .Select(
        loop =>
          new BrepLoop(
            spcklBrep,
            loop.Face.FaceIndex,
            loop.Trims.Select(t => t.TrimIndex).ToList(),
            (BrepLoopType)loop.LoopType
          )
      )
      .ToList();

    // Trims
    spcklBrep.Trims = brep.Trims
      .Select(trim =>
      {
        var t = new BrepTrim(
          spcklBrep,
          trim.Edge?.EdgeIndex ?? -1,
          trim.Face.FaceIndex,
          trim.Loop.LoopIndex,
          trim.TrimCurveIndex,
          (int)trim.IsoStatus,
          (BrepTrimType)trim.TrimType,
          trim.IsReversed(),
          trim.StartVertex.VertexIndex,
          trim.EndVertex.VertexIndex
        );
        t.Domain = IntervalToSpeckle(trim.Domain);

        return t;
      })
      .ToList();

    spcklBrep.volume = brep.IsSolid ? brep.GetVolume() : 0;
    spcklBrep.area = brep.GetArea();
    spcklBrep.bbox = BoxToSpeckle(new RH.Box(brep.GetBoundingBox(false)), u);

    return spcklBrep;
  }

  private RH.Mesh GetBrepDisplayMesh(RH.Brep brep)
  {
    var joinedMesh = new RH.Mesh();

    // get from settings
    //Settings.TryGetValue("sendMeshSetting", out string meshSetting);

    RH.MeshingParameters mySettings;
    switch (SelectedMeshSettings)
    {
      case MeshSettings.CurrentDoc:
        mySettings = RH.MeshingParameters.DocumentCurrentSetting(Doc);
        break;
      case MeshSettings.Default:
      default:
        mySettings = new RH.MeshingParameters(0.05, 0.05);
        break;
    }

    try
    {
      joinedMesh.Append(RH.Mesh.CreateFromBrep(brep, mySettings));
      return joinedMesh;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return null;
    }
  }

  /// <summary>
  /// Converts a Speckle <see cref="Brep"/> instance to a Rhino <see cref="Rhino.Geometry.Brep"/>
  /// </summary>
  /// <param name="brep">The Speckle Brep to convert</param>
  /// <returns></returns>
  /// <exception cref="Exception">Throws exception if the provenance is not Rhino</exception>
  public RH.Brep BrepToNative(Brep brep, out List<string> notes)
  {
    notes = new List<string>();
    var tol = Doc.ModelAbsoluteTolerance;
    try
    {
      // TODO: Provenance exception is meaningless now, must change for provenance build checks.
      // if (brep.provenance != Speckle.Core.Kits.Applications.Rhino6)
      //   throw new Exception("Unknown brep provenance: " + brep.provenance +
      //                       ". Don't know how to convert from one to the other.");

      var newBrep = new RH.Brep();
      brep.Curve3D.ForEach(crv => newBrep.AddEdgeCurve(CurveToNative(crv)));
      brep.Curve2D.ForEach(crv => newBrep.AddTrimCurve(CurveToNative(crv)));
      brep.Surfaces.ForEach(surf => newBrep.AddSurface(SurfaceToNative(surf)));
      brep.Vertices.ForEach(vert => newBrep.Vertices.Add(PointToNative(vert).Location, tol));
      brep.Edges.ForEach(edge =>
      {
        if (
          edge.Domain == null
          || edge.Domain.start == edge.Curve.domain.start && edge.Domain.end == edge.Curve.domain.end
        )
        {
          newBrep.Edges.Add(edge.Curve3dIndex);
        }
        else
        {
          newBrep.Edges.Add(edge.StartIndex, edge.EndIndex, edge.Curve3dIndex, IntervalToNative(edge.Domain), tol);
        }
      });
      brep.Faces.ForEach(face =>
      {
        var f = newBrep.Faces.Add(face.SurfaceIndex);
        f.OrientationIsReversed = face.OrientationReversed;
      });

      brep.Loops.ForEach(loop =>
      {
        var f = newBrep.Faces[loop.FaceIndex];
        var l = newBrep.Loops.Add((RH.BrepLoopType)loop.Type, f);
        loop.Trims
          .ToList()
          .ForEach(trim =>
          {
            RH.BrepTrim rhTrim;
            if (trim.EdgeIndex != -1)
            {
              rhTrim = newBrep.Trims.Add(
                newBrep.Edges[trim.EdgeIndex],
                trim.IsReversed,
                newBrep.Loops[trim.LoopIndex],
                trim.CurveIndex
              );
            }
            else if (trim.TrimType == BrepTrimType.Singular)
            {
              rhTrim = newBrep.Trims.AddSingularTrim(
                newBrep.Vertices[trim.EndIndex],
                newBrep.Loops[trim.LoopIndex],
                (RH.IsoStatus)trim.IsoStatus,
                trim.CurveIndex
              );
            }
            else
            {
              rhTrim = newBrep.Trims.Add(trim.IsReversed, newBrep.Loops[trim.LoopIndex], trim.CurveIndex);
            }

            rhTrim.IsoStatus = (RH.IsoStatus)trim.IsoStatus;
            rhTrim.TrimType = (RH.BrepTrimType)trim.TrimType;
            rhTrim.SetTolerances(tol, tol);
          });
      });

      newBrep.Repair(tol);

      return newBrep;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      notes.Add(ex.ToFormattedString());
      return null;
    }
  }

  // TODO: We're no longer creating new extrusions - they are converted as brep. This is here just for backwards compatibility.
  [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Obsolete")]
  [Obsolete("Unused")]
  public RH.Extrusion ExtrusionToNative(Extrusion extrusion)
  {
    RH.Curve outerProfile = CurveToNative((Curve)extrusion.profile);
    RH.Curve innerProfile = null;
    if (extrusion.profiles.Count == 2)
    {
      innerProfile = CurveToNative((Curve)extrusion.profiles[1]);
    }

    try
    {
      var IsClosed = extrusion.profile.GetType().GetProperty("IsClosed").GetValue(extrusion.profile, null) as bool?;
      if (IsClosed != true)
      {
        outerProfile.Reverse();
      }
    }
    catch { }

    var myExtrusion = RH.Extrusion.Create(
      outerProfile.ToNurbsCurve(),
      (double)extrusion.length,
      (bool)extrusion.capped
    );
    if (innerProfile != null)
    {
      myExtrusion.AddInnerProfile(innerProfile);
    }

    return myExtrusion;
  }

  // Proper explosion of polycurves:
  // (C) The Rutten David https://www.grasshopper3d.com/forum/topics/explode-closed-planar-curve-using-rhinocommon
  public bool CurveSegments(List<RH.Curve> L, RH.Curve crv, bool recursive)
  {
    if (crv == null)
    {
      return false;
    }

    RH.PolyCurve polycurve = crv as RH.PolyCurve;

    if (polycurve != null)
    {
      if (recursive)
      {
        polycurve.RemoveNesting();
      }

      RH.Curve[] segments = polycurve.Explode();

      if (segments == null)
      {
        return false;
      }

      if (segments.Length == 0)
      {
        return false;
      }

      if (recursive)
      {
        foreach (RH.Curve S in segments)
        {
          CurveSegments(L, S, recursive);
        }
      }
      else
      {
        foreach (RH.Curve S in segments)
        {
          L.Add(S.DuplicateShallow() as RH.Curve);
        }
      }

      return true;
    }

    //Nothing else worked, lets assume it's a nurbs curve and go from there...
    var nurbs = crv.ToNurbsCurve();
    if (nurbs == null)
    {
      return false;
    }

    double t0 = nurbs.Domain.Min;
    double t1 = nurbs.Domain.Max;
    double t;

    int LN = L.Count;

    do
    {
      if (!nurbs.GetNextDiscontinuity(RH.Continuity.C1_locus_continuous, t0, t1, out t))
      {
        break;
      }

      var trim = new RH.Interval(t0, t);
      if (trim.Length < 1e-10)
      {
        t0 = t;
        continue;
      }

      var M = nurbs.DuplicateCurve();
      M = M.Trim(trim);
      if (M.IsValid)
      {
        L.Add(M);
      }

      t0 = t;
    } while (true);

    if (L.Count == LN)
    {
      L.Add(nurbs);
    }

    return true;
  }

  public RH.NurbsSurface SurfaceToNative(Surface surface)
  {
    // Create rhino surface
    var points = surface
      .GetControlPoints()
      .Select(
        l =>
          l.Select(
              p =>
                new ControlPoint(
                  ScaleToNative(p.x, p.units),
                  ScaleToNative(p.y, p.units),
                  ScaleToNative(p.z, p.units),
                  p.weight,
                  p.units
                )
            )
            .ToList()
      )
      .ToList();

    var result = RH.NurbsSurface.Create(
      3,
      surface.rational,
      surface.degreeU + 1,
      surface.degreeV + 1,
      points.Count,
      points[0].Count
    );

    // Set knot vectors
    var correctUKnots = GetCorrectKnots(surface.knotsU, surface.countU, surface.degreeU);
    for (int i = 0; i < correctUKnots.Count; i++)
    {
      result.KnotsU[i] = correctUKnots[i];
    }

    var correctVKnots = GetCorrectKnots(surface.knotsV, surface.countV, surface.degreeV);
    for (int i = 0; i < correctVKnots.Count; i++)
    {
      result.KnotsV[i] = correctVKnots[i];
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

  public List<List<ControlPoint>> ControlPointsToSpeckle(NurbsSurfacePointList controlPoints, string units = null)
  {
    var points = new List<List<ControlPoint>>();
    for (var i = 0; i < controlPoints.CountU; i++)
    {
      var row = new List<ControlPoint>();
      for (var j = 0; j < controlPoints.CountV; j++)
      {
        var pt = controlPoints.GetControlPoint(i, j);
        var pos = pt.Location;
        row.Add(new ControlPoint(pos.X, pos.Y, pos.Z, pt.Weight, units ?? ModelUnits));
      }

      points.Add(row);
    }

    return points;
  }

  public Surface SurfaceToSpeckle(RH.NurbsSurface surface, string units = null)
  {
    var u = units ?? ModelUnits;
    var result = new Surface
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
    result.units = u;

    result.SetControlPoints(ControlPointsToSpeckle(surface.Points));
    result.bbox = BoxToSpeckle(new RH.Box(surface.GetBoundingBox(true)), u);

    return result;
  }

  private List<double> GetCorrectKnots(List<double> knots, int controlPointCount, int degree)
  {
    var correctKnots = knots;
    if (knots.Count == controlPointCount + degree + 1)
    {
      correctKnots.RemoveAt(0);
      correctKnots.RemoveAt(correctKnots.Count - 1);
    }

    return correctKnots;
  }
#if GRASSHOPPER
  // Interval2d
  public Interval2d Interval2dToSpeckle(UVInterval interval)
    {
      return new Interval2d(IntervalToSpeckle(interval.U), IntervalToSpeckle(interval.V));
    }

  public UVInterval Interval2dToNative(Interval2d interval)
    {
      return new UVInterval(IntervalToNative(interval.u), IntervalToNative(interval.v));
    }
#endif
}

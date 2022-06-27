using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Objects.Geometry;
using Objects.Other;
using Objects.Primitive;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Autodesk.Revit.ApplicationServices;
using Arc = Objects.Geometry.Arc;
using Curve = Objects.Geometry.Curve;
using DB = Autodesk.Revit.DB;
using Ellipse = Objects.Geometry.Ellipse;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Pointcloud = Objects.Geometry.Pointcloud;
using Spiral = Objects.Geometry.Spiral;
using Surface = Objects.Geometry.Surface;
using Units = Speckle.Core.Kits.Units;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Revit
{
  /// <summary>
  ///Internal helper methods used for converison
  /// </summary>
  public partial class ConverterRevit
  {
    // Convenience methods point:
    public double[] PointToArray(Point pt)
    {
      return new double[] { pt.x, pt.y, pt.z };
    }
    public List<double> PointsToFlatList(IEnumerable<Point> points)
    {
      return points.SelectMany(PointToArray).ToList();
    }

    public object GeometryToNative(Base geom)
    {
      switch (geom)
      {
        case Point g:
          return PointToNative(g);
        case ICurve g:
          return CurveToNative(g);
        case Plane g:
          return PlaneToNative(g);
        case Vector g:
          return VectorToNative(g);

        default:
          throw new Speckle.Core.Logging.SpeckleException("Cannot convert Curve of type " + geom.GetType());
      }
    }

    public XYZ PointToNative(Point pt)
    {
      var revitPoint = new XYZ(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units));
      var intPt = ToInternalCoordinates(revitPoint, true);
      return intPt;
    }

    public Point PointToSpeckle(XYZ pt, string units = null)
    {
      var u = units ?? ModelUnits;
      var extPt = ToExternalCoordinates(pt, true);

      var pointToSpeckle = new Point(
        u == Units.None ? extPt.X : ScaleToSpeckle(extPt.X),
        u == Units.None ? extPt.Y : ScaleToSpeckle(extPt.Y),
        u == Units.None ? extPt.Z : ScaleToSpeckle(extPt.Z),
        u);
      return pointToSpeckle;
    }

    public List<XYZ> PointListToNative(IList<double> arr, string units = null)
    {
      if (arr.Count % 3 != 0) throw new SpeckleException("Array malformed: length%3 != 0.");

      var u = units ?? ModelUnits;

      var points = new List<XYZ>(arr.Count / 3);
      for (int i = 2; i < arr.Count; i += 3)
        points.Add(new XYZ(
          ScaleToNative(arr[i - 2], u),
          ScaleToNative(arr[i - 1], u),
          ScaleToNative(arr[i], u)));

      return points;
    }

    public Pointcloud PointcloudToSpeckle(PointCloudInstance pointcloud, string units = null)
    {
      var u = units ?? ModelUnits;
      var boundingBox = pointcloud.get_BoundingBox(null);
      var filter = PointCloudFilterFactory.CreateMultiPlaneFilter(new List<DB.Plane>() { DB.Plane.CreateByNormalAndOrigin(XYZ.BasisZ, boundingBox.Min) });
      var points = pointcloud.GetPoints(filter, 0.0001, 999999); // max limit is 1 mil but 1000000 throws error

      var _pointcloud = new Pointcloud();
      _pointcloud.points = points.Select(o => PointToSpeckle(o, u)).SelectMany(o => new List<double>() { o.x, o.y, o.z }).ToList();
      _pointcloud.colors = points.Select(o => o.Color).ToList();
      _pointcloud.units = u;
      _pointcloud.bbox = BoxToSpeckle(boundingBox, u);

      return _pointcloud;
    }

    public Vector VectorToSpeckle(XYZ pt, string units = null)
    {
      var u = units ?? ModelUnits;
      var extPt = ToExternalCoordinates(pt, false);
      var pointToSpeckle = new Vector(
        u == Units.None ? extPt.X : ScaleToSpeckle(extPt.X),
        u == Units.None ? extPt.Y : ScaleToSpeckle(extPt.Y),
        u == Units.None ? extPt.Z : ScaleToSpeckle(extPt.Z),
        u);
      return pointToSpeckle;
    }

    public XYZ VectorToNative(Vector pt)
    {
      var revitVector = new XYZ(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units));
      var intV = ToInternalCoordinates(revitVector, false);
      return intV;
    }

    public DB.Plane PlaneToNative(Plane plane)
    {
      return DB.Plane.CreateByOriginAndBasis(PointToNative(plane.origin), VectorToNative(plane.xdir).Normalize(), VectorToNative(plane.ydir).Normalize());
    }


    public Plane PlaneToSpeckle(DB.Plane plane, string units = null)
    {
      var u = units ?? ModelUnits;
      var origin = PointToSpeckle(plane.Origin, u);
      var normal = VectorToSpeckle(plane.Normal, u);
      var xdir = VectorToSpeckle(plane.XVec, u);
      var ydir = VectorToSpeckle(plane.YVec, u);

      return new Plane(origin, normal, xdir, ydir, u);
    }

    public DB.Line LineToNative(Line line)
    {
      return DB.Line.CreateBound(
        PointToNative(line.start),
        PointToNative(line.end));
    }

    public Line LineToSpeckle(DB.Line line, string units = null)
    {
      var u = units ?? ModelUnits;
      var l = new Line { units = u };
      l.start = PointToSpeckle(line.GetEndPoint(0), u);
      l.end = PointToSpeckle(line.GetEndPoint(1), u);
      l.domain = new Interval(line.GetEndParameter(0), line.GetEndParameter(1));

      l.length = ScaleToSpeckle(line.Length);
      return l;
    }

    public Circle CircleToSpeckle(DB.Arc arc, string units = null)
    {
      // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
      var u = units ?? ModelUnits;
      var arcPlane = DB.Plane.CreateByNormalAndOrigin(arc.Normal, arc.Center);

      var c = new Circle(PlaneToSpeckle(arcPlane, u), u == Units.None ? arc.Radius : ScaleToSpeckle(arc.Radius), u);
      c.length = ScaleToSpeckle(arc.Length);
      return c;
    }

    public DB.Arc CircleToNative(Circle circle)
    {
      var plane = PlaneToNative(circle.plane);
      return DB.Arc.Create(plane, ScaleToNative((double)circle.radius, circle.units), 0, 2 * Math.PI);
    }

    public DB.Arc ArcToNative(Arc arc)
    {
      double startAngle, endAngle;
      if (arc.startAngle > arc.endAngle)
      {
        startAngle = (double)arc.endAngle;
        endAngle = (double)arc.startAngle;
      }
      else
      {
        startAngle = (double)arc.startAngle;
        endAngle = (double)arc.endAngle;
      }
      var plane = PlaneToNative(arc.plane);

      if (Point.Distance(arc.startPoint, arc.endPoint) < 1E-6)
      {
        // Endpoints coincide, it's a circle.
        return DB.Arc.Create(plane, ScaleToNative(arc.radius ?? 0, arc.units), startAngle, endAngle);
      }
      
      return DB.Arc.Create(PointToNative(arc.startPoint), PointToNative(arc.endPoint), PointToNative(arc.midPoint));
      //return Arc.Create( plane.Origin, (double) arc.Radius * Scale, startAngle, endAngle, plane.XVec, plane.YVec );
    }

    public Arc ArcToSpeckle(DB.Arc arc, string units = null)
    {
      var u = units ?? ModelUnits;
      // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
      var arcPlane = DB.Plane.CreateByOriginAndBasis(arc.Center, arc.XDirection, arc.YDirection);
      XYZ center = arc.Center;

      XYZ dir0 = (arc.GetEndPoint(0) - center).Normalize();
      XYZ dir1 = (arc.GetEndPoint(1) - center).Normalize();

      XYZ start = arc.Evaluate(0, true);
      XYZ end = arc.Evaluate(1, true);
      XYZ mid = arc.Evaluate(0.5, true);

      double startAngle = arc.XDirection.AngleOnPlaneTo(dir0, arc.Normal);
      double endAngle = arc.XDirection.AngleOnPlaneTo(dir1, arc.Normal);

      var a = new Arc(PlaneToSpeckle(arcPlane, u), u == Units.None ? arc.Radius : ScaleToSpeckle(arc.Radius), startAngle, endAngle, endAngle - startAngle, u);
      a.endPoint = PointToSpeckle(end, u);
      a.startPoint = PointToSpeckle(start, u);
      a.midPoint = PointToSpeckle(mid, u);
      a.length = ScaleToSpeckle(arc.Length);
      a.domain = new Interval(arc.GetEndParameter(0), arc.GetEndParameter(1));

      return a;
    }

    public DB.Curve EllipseToNative(Ellipse ellipse)
    {
      //TODO: support ellipse arcs
      using (DB.Plane basePlane = PlaneToNative(ellipse.plane))
      {
        var e = DB.Ellipse.CreateCurve(
          PointToNative(ellipse.plane.origin),
          ScaleToNative((double)ellipse.firstRadius, ellipse.units),
          ScaleToNative((double)ellipse.secondRadius, ellipse.units),
          basePlane.XVec.Normalize(),
          basePlane.YVec.Normalize(),
          0,
          2 * Math.PI
        );
        e.MakeBound(ellipse.trimDomain?.start ?? 0, ellipse.trimDomain?.end ?? 2 * Math.PI);
        return e;
      }
    }

    public Ellipse EllipseToSpeckle(DB.Ellipse ellipse, string units = null)
    {
      var u = units ?? ModelUnits;
      using (DB.Plane basePlane = DB.Plane.CreateByOriginAndBasis(ellipse.Center, ellipse.XDirection, ellipse.YDirection))
      {
        var trim = ellipse.IsBound ? new Interval(ellipse.GetEndParameter(0), ellipse.GetEndParameter(1)) : null;

        var ellipseToSpeckle = new Ellipse(
          PlaneToSpeckle(basePlane, u),
          u == Units.None ? ellipse.RadiusX : ScaleToSpeckle(ellipse.RadiusX),
          u == Units.None ? ellipse.RadiusY : ScaleToSpeckle(ellipse.RadiusY),
          new Interval(0, 2 * Math.PI),
          trim,
          u);
        ellipseToSpeckle.length = ScaleToSpeckle(ellipse.Length);
        ellipseToSpeckle.domain = new Interval(0, 1);
        return ellipseToSpeckle;
      }
    }

    public Curve NurbsToSpeckle(DB.NurbSpline revitCurve, string units = null)
    {
      var points = new List<double>();
      foreach (var p in revitCurve.CtrlPoints)
      {
        var point = PointToSpeckle(p, units);
        points.AddRange(new List<double> { point.x, point.y, point.z });
      }

      Curve speckleCurve = new Curve();
      speckleCurve.weights = revitCurve.Weights.Cast<double>().ToList();
      speckleCurve.points = points;
      speckleCurve.knots = revitCurve.Knots.Cast<double>().ToList(); ;
      speckleCurve.degree = revitCurve.Degree;
      //speckleCurve.periodic = revitCurve.Period;
      speckleCurve.rational = revitCurve.isRational;
      speckleCurve.closed = RevitVersionHelper.IsCurveClosed(revitCurve);
      speckleCurve.units = units ?? ModelUnits;
      speckleCurve.domain = new Interval(revitCurve.GetEndParameter(0), revitCurve.GetEndParameter(1));
      speckleCurve.length = ScaleToSpeckle(revitCurve.Length);

      var coords = revitCurve.Tessellate().SelectMany(xyz => PointToSpeckle(xyz, units).ToList()).ToList();
      speckleCurve.displayValue = new Polyline(coords, units);

      return speckleCurve;
    }

    public DB.Curve CurveToNative(Curve speckleCurve)
    {
      var pts = new List<XYZ>();
      for (int i = 0; i < speckleCurve.points.Count; i += 3)
      {
        //use PointToNative for conversion as that takes into account the Project Base Point
        var point = new Point(speckleCurve.points[i], speckleCurve.points[i + 1], speckleCurve.points[i + 2], speckleCurve.units);
        pts.Add(PointToNative(point));
      }
      try
      {
        if (speckleCurve.knots != null && speckleCurve.weights != null && speckleCurve.knots.Any() && speckleCurve.weights.Any())
        {
          var weights = speckleCurve.weights.GetRange(0, pts.Count);
          var speckleKnots = new List<double>(speckleCurve.knots);
          if (speckleKnots.Count != pts.Count + speckleCurve.degree + 1)
          {
            // Curve has rhino knots, repeat first and last.
            speckleKnots.Insert(0, speckleKnots[0]);
            speckleKnots.Add(speckleKnots[speckleKnots.Count - 1]);
          }

          //var knots = speckleKnots.GetRange(0, pts.Count + speckleCurve.degree + 1);
          var curve = NurbSpline.CreateCurve(speckleCurve.degree, speckleKnots, pts, weights);
          return curve;
        }
        else
        {
          var weights = speckleCurve.weights.GetRange(0, pts.Count);
          var curve = NurbSpline.CreateCurve(pts, weights);
          return curve;
        }

      }
      catch (Exception e)
      {
        if (e is Autodesk.Revit.Exceptions.ArgumentException) throw e; // prob a closed, periodic curve
        return null;
      }
    }

    public CurveArray CurveToNative(List<ICurve> crvs)
    {
      CurveArray crvsArray = new CurveArray();
      foreach (var crv in crvs)
      {
        var crvEnumerator = CurveToNative(crv).GetEnumerator();
        while (crvEnumerator.MoveNext() && crvEnumerator.Current != null)
          crvsArray.Append(crvEnumerator.Current as DB.Curve);
      }
      return crvsArray;
    }

    /// <summary>
    /// Recursively creates an ordered list of curves from a polycurve/polyline.
    /// Please note that a polyline is broken down into lines.
    /// </summary>
    /// <param name="crv">A speckle curve.</param>
    /// <returns></returns>
    public CurveArray CurveToNative(ICurve crv, bool splitIfClosed = false)
    {
      CurveArray curveArray = new CurveArray();
      switch (crv)
      {
        case Line line:
          curveArray.Append(LineToNative(line));
          return curveArray;

        case Arc arc:
          curveArray.Append(ArcToNative(arc));
          return curveArray;

        case Circle circle:
          curveArray.Append(CircleToNative(circle));
          return curveArray;

        case Ellipse ellipse:
          curveArray.Append(EllipseToNative(ellipse));
          return curveArray;

        case Spiral spiral:
          return PolylineToNative(spiral.displayValue);

        case Curve nurbs:
          var n = CurveToNative(nurbs);
          if(IsCurveClosed(n) && splitIfClosed)
          {
            var split = SplitCurveInTwoHalves(n);
            curveArray.Append(split.Item1);
            curveArray.Append(split.Item2);
          }
          else
          {
            curveArray.Append(n);
          }
          return curveArray;

        case Polyline poly:
          return PolylineToNative(poly);

        case Polycurve plc:
          foreach (var seg in plc.segments)
          {
            // Enumerate all curves in the array to ensure polylines get fully converted.
            var crvEnumerator = CurveToNative(seg).GetEnumerator();
            while (crvEnumerator.MoveNext() && crvEnumerator.Current != null)
              curveArray.Append(crvEnumerator.Current as DB.Curve);
          }
          return curveArray;
        default:
          throw new Speckle.Core.Logging.SpeckleException("The provided geometry is not a valid curve");
      }
    }
    
    //thanks Revit
    public CurveLoop CurveArrayToCurveLoop(CurveArray array)
    {
      var loop = new CurveLoop();
      UnboundCurveIfSingle(array);
      foreach (var item in array.Cast<DB.Curve>())
        loop.Append(item);
      return loop;
    }

    public ICurve CurveToSpeckle(DB.Curve curve, string units = null)
    {
      var u = units ?? ModelUnits;
      switch (curve)
      {
        case DB.Line line:
          return LineToSpeckle(line, u);
        case DB.Arc arc:
          if (!arc.IsBound)
          {
            return (CircleToSpeckle(arc, u));
          }
          return ArcToSpeckle(arc, u);
        case DB.Ellipse ellipse:
          return EllipseToSpeckle(ellipse, u);
        case DB.NurbSpline nurbs:
          return NurbsToSpeckle(nurbs, u);
        case DB.HermiteSpline spline:
          return HermiteSplineToSpeckle(spline, u);
        default:
          throw new Speckle.Core.Logging.SpeckleException("Cannot convert Curve of type " + curve.GetType());
      }
    }

    public Polycurve CurveListToSpeckle(IList<DB.Curve> loop, string units = null)
    {
      var polycurve = new Polycurve();
      polycurve.units = units ?? ModelUnits;
      polycurve.closed = loop.First().GetEndPoint(0).DistanceTo(loop.Last().GetEndPoint(1)) < TOLERANCE;
      polycurve.length = ScaleToSpeckle(loop.Sum(x => x.Length));
      polycurve.segments.AddRange(loop.Select(x => CurveToSpeckle(x)));
      return polycurve;
    }

    public Polycurve CurveLoopToSpeckle(CurveLoop loop, string units = null)
    {
      var polycurve = new Polycurve();
      polycurve.units = units ?? ModelUnits;
      polycurve.closed = !loop.IsOpen();
      polycurve.length = units == Units.None ? loop.GetExactLength() : ScaleToSpeckle(loop.GetExactLength());

      polycurve.segments.AddRange(loop.Select(x => CurveToSpeckle(x)));
      return polycurve;
    }

    private ICurve HermiteSplineToSpeckle(HermiteSpline spline, string units = null)
    {
      var nurbs = DB.NurbSpline.Create(spline);
      return NurbsToSpeckle(nurbs, units ?? ModelUnits);
    }

    /// <summary>
    /// Converts a Speckle <see cref="Polyline"/> into a <see cref="CurveArray"/>.
    /// </summary>
    /// <remarks>
    /// This method will ensure that no lines smaller than the allowed length are created.
    /// If small segments are encountered, the geometry will be modified to ensure all segments have minimum length and remain connected.
    /// This will result in some vertices being ignored during conversion, which are logged in the report.
    /// </remarks>
    /// <param name="polyline">The Speckle <see cref="Polyline"/> to convert to Revit</param>
    /// <returns>A Revit <see cref="CurveArray"/></returns>
    public CurveArray PolylineToNative(Polyline polyline)
    {
      var curveArray = new CurveArray();
      if (polyline.value.Count == 6)
      {
        // Polyline is actually a single line
        TryAppendLineSafely(
          curveArray, 
          new Line(polyline.value, polyline.units)
        );
      }
      else
      {
        var pts = polyline.GetPoints();
        var lastPt = pts[0];
        for (var i = 1; i < pts.Count; i++)
        {
          var success = TryAppendLineSafely(
            curveArray, 
            new Line(lastPt, pts[i] , polyline.units)
          );
          if(success) lastPt = pts[i];
        }

        if (polyline.closed)
        {
          TryAppendLineSafely(
            curveArray, 
            new Line(pts[pts.Count - 1], pts[0] , polyline.units)
          );
        }
      }
      return curveArray;
    }


    public Polyline PolylineToSpeckle(PolyLine polyline, string units = null)
    {
      var coords = polyline.GetCoordinates().SelectMany(coord => PointToSpeckle(coord).ToList()).ToList();
      return new Polyline(coords, units ?? ModelUnits);
    }

    public Box BoxToSpeckle(DB.BoundingBoxXYZ box, string units = null)
    {
      // convert min and max pts to speckle first
      var min = PointToSpeckle(box.Min, units);
      var max = PointToSpeckle(box.Max, units);

      // get the base plane of the bounding box from the transform
      var transform = box.Transform;
      var plane = DB.Plane.CreateByOriginAndBasis(transform.Origin, transform.BasisX.Normalize(), transform.BasisY.Normalize());

      var _box = new Box()
      {
        xSize = new Interval(min.x, max.x),
        ySize = new Interval(min.y, max.y),
        zSize = new Interval(min.z, max.z),
        basePlane = PlaneToSpeckle(plane),
        units = units ?? ModelUnits
      };

      return _box;
    }

    public DB.BoundingBoxXYZ BoxToNative(Box box)
    {
      var boundingBox = new BoundingBoxXYZ();
      boundingBox.Min = PointToNative(new Point((double)box.xSize.start, (double)box.ySize.start, (double)box.zSize.start));
      boundingBox.Max = PointToNative(new Point((double)box.xSize.end, (double)box.ySize.end, (double)box.zSize.end));
      return boundingBox;
    }

    public Mesh MeshToSpeckle(DB.Mesh mesh, Document d, string units = null)
    {
      var vertices = new List<double>(mesh.Vertices.Count * 3);
      foreach (var vert in mesh.Vertices)
        vertices.AddRange(PointToSpeckle(vert).ToList());

      var faces = new List<int>(mesh.NumTriangles * 4);
      for (int i = 0; i < mesh.NumTriangles; i++)
      {
        var triangle = mesh.get_Triangle(i);
        var A = triangle.get_Index(0);
        var B = triangle.get_Index(1);
        var C = triangle.get_Index(2);
        faces.Add(0);
        faces.AddRange(new int[]
        {
          (int)A, (int)B, (int)C
        });
      }

      var u = units ?? ModelUnits;
      var speckleMesh = new Mesh(vertices, faces, units: u)
      {
        ["renderMaterial"] = RenderMaterialToSpeckle(d.GetElement(mesh.MaterialElementId) as DB.Material)
      };

      return speckleMesh;
    }

    // Insipred by
    // https://github.com/DynamoDS/DynamoRevit/blob/master/src/Libraries/RevitNodes/GeometryConversion/ProtoToRevitMesh.cs
    public IList<GeometryObject> MeshToNative(Mesh mesh, TessellatedShapeBuilderTarget target = TessellatedShapeBuilderTarget.Mesh, TessellatedShapeBuilderFallback fallback = TessellatedShapeBuilderFallback.Salvage, RenderMaterial parentMaterial = null)
    {
      var tsb = new TessellatedShapeBuilder() { Fallback = fallback, Target = target, GraphicsStyleId = ElementId.InvalidElementId };

      var valid = tsb.AreTargetAndFallbackCompatible(target, fallback);
      tsb.OpenConnectedFaceSet(target == TessellatedShapeBuilderTarget.Solid);

      var vertices = ArrayToPoints(mesh.vertices, mesh.units);

      ElementId materialId = RenderMaterialToNative(parentMaterial ?? (mesh["renderMaterial"] as RenderMaterial));
      
      int i = 0;
      while (i < mesh.faces.Count)
      {
        int n = mesh.faces[i];
        if (n < 3) n += 3; // 0 -> 3, 1 -> 4 to preserve backwards compatibility

        var points = mesh.faces.GetRange(i + 1, n).Select(x => vertices[x]).ToArray();

        if (IsNonPlanarQuad(points))
        {
          //Non-planar quads will be triangulated as it's more desirable than `TessellatedShapeBuilder.Build`'s attempt to make them planar.
          //TODO consider triangulating all n > 3 polygons that are non-planar
          var triPoints = new List<XYZ> { points[0], points[1], points[3] };
          var face1 = new TessellatedFace(triPoints, materialId);
          tsb.AddFace(face1);

          triPoints = new List<XYZ> { points[1], points[2], points[3] }; ;
          var face2 = new TessellatedFace(triPoints, materialId);
          tsb.AddFace(face2);
        }
        else
        {
          var face = new TessellatedFace(points, materialId);
          tsb.AddFace(face);
        }

        i += n + 1;
      }

      tsb.CloseConnectedFaceSet();
      try
      {
        tsb.Build();
      }
      catch (Exception e)
      {
        Report.LogConversionError(e);
        return null;
      }
      var result = tsb.GetBuildResult();
      return result.GetGeometricalObjects();


      static bool IsNonPlanarQuad(IList<XYZ> points)
      {
        if (points.Count != 4) return false;

        var matrix = new Matrix4x4(
          (float)points[0].X, (float)points[1].X, (float)points[2].X, (float)points[3].X,
          (float)points[0].Y, (float)points[1].Y, (float)points[2].Y, (float)points[3].Y,
          (float)points[0].Z, (float)points[1].Z, (float)points[2].Z, (float)points[3].Z,
          1, 1, 1, 1
        );
        return matrix.GetDeterminant() != 0;
      }
    }

    public XYZ[] ArrayToPoints(IList<double> arr, string units = null)
    {
      if (arr.Count % 3 != 0)
        throw new Speckle.Core.Logging.SpeckleException("Array malformed: length%3 != 0.");

      XYZ[] points = new XYZ[arr.Count / 3];

      for (int i = 2, k = 0; i < arr.Count; i += 3)
      {
        var point = new Point(arr[i - 2], arr[i - 1], arr[i], units);
        points[k++] = PointToNative(point);
      }

      return points;
    }

    //https://github.com/DynamoDS/DynamoRevit/blob/f8206726d8a3aa5bf06f5dbf7ce8a732bb025c34/src/Libraries/RevitNodes/GeometryConversion/GeometryPrimitiveConverter.cs#L201
    public XYZ GetPerpendicular(XYZ xyz)
    {
      var ixn = xyz.Normalize();
      var xn = new XYZ(1, 0, 0);

      if (ixn.IsAlmostEqualTo(xn))
        xn = new XYZ(0, 1, 0);

      return ixn.CrossProduct(xn).Normalize();
    }

    public Geometry.Surface FaceToSpeckle(DB.Face face, DB.BoundingBoxUV uvBox, string units = null)
    {

#if (REVIT2021 || REVIT2022 || REVIT2023)
      var surf = DB.ExportUtils.GetNurbsSurfaceDataForSurface(face.GetSurface());
#else
      var surf = DB.ExportUtils.GetNurbsSurfaceDataForFace(face);
#endif
      var spcklSurface = NurbsSurfaceToSpeckle(surf, face.GetBoundingBox(), units ?? ModelUnits);
      return spcklSurface;
    }

    public Surface NurbsSurfaceToSpeckle(DB.NurbsSurfaceData surface, DB.BoundingBoxUV uvBox, string units = null)
    {
      var result = new Surface();

      var unit = units ?? ModelUnits;
      result.units = unit;

      result.degreeU = surface.DegreeU;
      result.degreeV = surface.DegreeV;

      result.domainU = new Interval(0, 1);
      result.domainV = new Interval(0, 1);

      var knotsU = surface.GetKnotsU().ToList();
      var knotsV = surface.GetKnotsV().ToList();

      result.knotsU = knotsU.GetRange(1, knotsU.Count - 2);
      result.knotsV = knotsV.GetRange(1, knotsV.Count - 2);

      var controlPointCountU = knotsU.Count - result.degreeU - 1;
      var controlPointCountV = knotsV.Count - result.degreeV - 1;

      var controlPoints = surface.GetControlPoints();
      var weights = surface.GetWeights();

      var points = new List<List<ControlPoint>>();
      for (var u = 0; u < controlPointCountU; u++)
      {
        var uOffset = u * controlPointCountV;
        var row = new List<ControlPoint>();

        for (var v = 0; v < controlPointCountV; v++)
        {
          var pt = controlPoints[uOffset + v];
          var extPt = ToExternalCoordinates(pt, true);
          if (surface.IsRational)
          {
            var w = weights[uOffset + v];
            var point = PointToSpeckle(extPt, unit);
            row.Add(new ControlPoint(point.x, point.y, point.z, w, unit));
          }
          else
          {
            var point = PointToSpeckle(extPt, unit);
            row.Add(new ControlPoint(point.x, point.y, point.z, unit));
          }
        }
        points.Add(row);
      }

      result.SetControlPoints(points);

      return result;
    }

    public List<BRepBuilderEdgeGeometry> BrepEdgeToNative(BrepEdge edge)
    {
      // TODO: Trim curve with domain. Unsure if this is necessary as all our curves are converted to NURBS on Rhino output.

      var nativeCurveArray = CurveToNative(edge.Curve);
      bool isTrimmed = edge.Curve.domain != null && edge.Domain != null &&
        (edge.Curve.domain.start != edge.Domain.start ||
          edge.Curve.domain.end != edge.Domain.end);
      if (nativeCurveArray.Size == 1)
      {
        var nativeCurve = nativeCurveArray.get_Item(0);

        if (edge.ProxyCurveIsReversed)
          nativeCurve = nativeCurve.CreateReversed();

        if (nativeCurve == null)
          return new List<BRepBuilderEdgeGeometry>();
        if (isTrimmed)
          nativeCurve.MakeBound(edge.Domain.start ?? 0, edge.Domain.end ?? 1);
        if (!nativeCurve.IsBound)
          nativeCurve.MakeBound(0, nativeCurve.Period);

        if (IsCurveClosed(nativeCurve))
        {
          var (first, second) = SplitCurveInTwoHalves(nativeCurve);
          if (edge.ProxyCurveIsReversed)
          {
            first = first.CreateReversed();
            second = second.CreateReversed();
          }
          var halfEdgeA = BRepBuilderEdgeGeometry.Create(first);
          var halfEdgeB = BRepBuilderEdgeGeometry.Create(second);
          return edge.ProxyCurveIsReversed 
            ? new List<BRepBuilderEdgeGeometry> { halfEdgeA, halfEdgeB }
            : new List<BRepBuilderEdgeGeometry> { halfEdgeB, halfEdgeA };
        }

        // TODO: Remove short segments if smaller than 'Revit.ShortCurveTolerance'.
        var fullEdge = BRepBuilderEdgeGeometry.Create(nativeCurve);
        return new List<BRepBuilderEdgeGeometry> { fullEdge };
      }

      var iterator = edge.ProxyCurveIsReversed ?
        nativeCurveArray.ReverseIterator() :
        nativeCurveArray.ForwardIterator();

      var result = new List<BRepBuilderEdgeGeometry>();
      while (iterator.MoveNext())
      {
        var crv = iterator.Current as DB.Curve;
        if (edge.ProxyCurveIsReversed)
          crv = crv.CreateReversed();
        result.Add(BRepBuilderEdgeGeometry.Create(crv));
      }

      return result;
    }

    public double[] ControlPointWeightsToNative(List<List<ControlPoint>> controlPoints)
    {
      var uCount = controlPoints.Count;
      var vCount = controlPoints[0].Count;
      var count = uCount * vCount;
      var weights = new double[count];
      int p = 0;

      controlPoints.ForEach(row =>
        row.ForEach(pt =>
          weights[p++] = pt.weight));

      return weights;
    }

    public XYZ[] ControlPointsToNative(List<List<ControlPoint>> controlPoints)
    {
      var uCount = controlPoints.Count;
      var vCount = controlPoints[0].Count;
      var count = uCount * vCount;
      var points = new DB.XYZ[count];
      int p = 0;

      controlPoints.ForEach(row =>
        row.ForEach(pt =>
        {
          var point = new Point(pt.x, pt.y, pt.z, pt.units);
          points[p++] = PointToNative(point);
        }));

      return points;
    }

    public double[] SurfaceKnotsToNative(List<double> list)
    {
      var count = list.Count;
      var knots = new double[count + 2];

      int j = 0, k = 0;
      while (j < count)
        knots[++k] = list[j++];

      knots[0] = knots[1];
      knots[count + 1] = knots[count];

      return knots;
    }

    public BRepBuilderSurfaceGeometry SurfaceToNative(Surface surface)
    {
      var uvBox = new DB.BoundingBoxUV(surface.knotsU[0], surface.knotsV[0], surface.knotsU[surface.knotsU.Count - 1], surface.knotsV[surface.knotsV.Count - 1]);
      var surfPts = surface.GetControlPoints();
      var uKnots = SurfaceKnotsToNative(surface.knotsU);
      var vKnots = SurfaceKnotsToNative(surface.knotsV);
      var cPts = ControlPointsToNative(surfPts);

      BRepBuilderSurfaceGeometry result;
      if (!surface.rational)
      {
        result = DB.BRepBuilderSurfaceGeometry.CreateNURBSSurface(surface.degreeU, surface.degreeV, uKnots,
          vKnots, cPts, false, uvBox);
      }
      else
      {
        var weights = ControlPointWeightsToNative(surfPts);
        result = DB.BRepBuilderSurfaceGeometry.CreateNURBSSurface(surface.degreeU, surface.degreeV, uKnots,
          vKnots, cPts, weights, false, uvBox);
      }

      return result;
    }

    public Solid BrepToNative(Brep brep)
    {
      //Make sure face references are calculated by revit

      var bRepType = BRepType.OpenShell;
      switch (brep.Orientation)
      {
        case BrepOrientation.Inward:
          bRepType = BRepType.Void;
          break;
        case BrepOrientation.Outward:
          bRepType = BRepType.Solid;
          break;
      }

      var materialId = RenderMaterialToNative(brep["renderMaterial"] as RenderMaterial);
      using var builder = new BRepBuilder(bRepType);

      builder.SetAllowShortEdges();
      builder.AllowRemovalOfProblematicFaces();

      var brepEdges = new List<DB.BRepBuilderGeometryId>[brep.Edges.Count];
      foreach (var face in brep.Faces)
      {
        var faceId = builder.AddFace(SurfaceToNative(face.Surface), face.OrientationReversed);
        builder.SetFaceMaterialId(faceId, materialId);
        
        foreach (var loop in face.Loops)
        {
          var loopId = builder.AddLoop(faceId);
          if (face.OrientationReversed)
            loop.TrimIndices.Reverse();

          foreach (var trim in loop.Trims)
          {
            if (trim.TrimType != BrepTrimType.Boundary && trim.TrimType != BrepTrimType.Mated && trim.TrimType != BrepTrimType.Seam)
              continue;

            if (trim.Edge == null)
              continue;

            var edgeIds = brepEdges[trim.EdgeIndex];
            if (edgeIds == null)
            {
              // First time we see this edge, convert it and add
              edgeIds = brepEdges[trim.EdgeIndex] = new List<BRepBuilderGeometryId>();
              var bRepBuilderGeometryIds = BrepEdgeToNative(trim.Edge).Select(edge => builder.AddEdge(edge));
              edgeIds.AddRange(bRepBuilderGeometryIds);
            }

            var trimReversed = face.OrientationReversed ? !trim.IsReversed : trim.IsReversed;
            if (trimReversed)
            {
              for (int e = edgeIds.Count - 1; e >= 0; --e)
                if (builder.IsValidEdgeId(edgeIds[e]))
                  builder.AddCoEdge(loopId, edgeIds[e], true);

            }
            else
            {
              for (int e = 0; e < edgeIds.Count; ++e)
                if (builder.IsValidEdgeId(edgeIds[e]))
                  builder.AddCoEdge(loopId, edgeIds[e], false);
            }
          }

          builder.FinishLoop(loopId);
        }
        builder.FinishFace(faceId);
      }

      var removedFace = builder.RemovedSomeFaces();
      var bRepBuilderOutcome = builder.Finish();
      if (bRepBuilderOutcome == BRepBuilderOutcome.Failure) return null;

      var isResultAvailable = builder.IsResultAvailable();
      if (!isResultAvailable) return null;
      var result = builder.GetResult();
      return result;
    }

    public Brep BrepToSpeckle(Solid solid, Document d, string units = null)
    {
#if REVIT2019 || REVIT2020
      throw new Speckle.Core.Logging.SpeckleException("Converting BREPs to Speckle is currently only supported in Revit 2021 and above.");
#else
      // TODO: Incomplete implementation!!
      var u = units ?? ModelUnits;
      var brep = new Brep();
      brep.units = u;

      if (solid is null || solid.Faces.IsEmpty) return null;

      var faceIndex = 0;
      var edgeIndex = 0;
      var curve2dIndex = 0;
      var curve3dIndex = 0;
      var loopIndex = 0;
      var trimIndex = 0;
      var surfaceIndex = 0;

      var speckleFaces = new Dictionary<Face, BrepFace>();
      var speckleEdges = new Dictionary<Edge, BrepEdge>();
      var speckleEdgeIndexes = new Dictionary<Edge, int>();
      var speckle3dCurves = new ICurve[solid.Edges.Size];
      var speckle2dCurves = new List<ICurve>();
      var speckleLoops = new List<BrepLoop>();
      var speckleTrims = new List<BrepTrim>();

      foreach (var face in solid.Faces.Cast<Face>())
      {
        var surface = FaceToSpeckle(face, out bool orientation, 0.0);
        var iterator = face.EdgeLoops.ForwardIterator();
        var loopIndices = new List<int>();

        while (iterator.MoveNext())
        {
          var loop = iterator.Current as EdgeArray;
          var loopTrimIndices = new List<int>();
          // Loop through the edges in the loop.
          var loopIterator = loop.ForwardIterator();
          while (loopIterator.MoveNext())
          {
            // Each edge should create a 2d curve, a 3d curve, a BrepTrim and a BrepEdge.
            var edge = loopIterator.Current as Edge;
            var faceA = edge.GetFace(0);

            // Determine what face side are we currently on.
            var edgeSide = face == faceA ? 0 : 1;

            // Get curve, create trim and save index
            var trim = edge.GetCurveUV(edgeSide);
            var sTrim = new BrepTrim(brep, edgeIndex, faceIndex, loopIndex, curve2dIndex, 0, BrepTrimType.Boundary, edge.IsFlippedOnFace(edgeSide), -1, -1);
            var sTrimIndex = trimIndex;
            loopTrimIndices.Add(sTrimIndex);

            // Add curve and trim, increase index counters.
            speckle2dCurves.Add(CurveToSpeckle(trim.As3DCurveInXYPlane(), Units.None));
            speckleTrims.Add(sTrim);
            curve2dIndex++;
            trimIndex++;

            // Check if we have visited this edge before.
            if (!speckleEdges.ContainsKey(edge))
            {
              // First time we visit this edge, add 3d curve and create new BrepEdge.
              var edgeCurve = edge.AsCurve();
              speckle3dCurves[curve3dIndex] = CurveToSpeckle(edgeCurve, u);
              var sCurveIndex = curve3dIndex;
              curve3dIndex++;

              // Create a trim with just one of the trimIndices set, the second one will be set on the opposite condition.
              var sEdge = new BrepEdge(brep, sCurveIndex, new[] { sTrimIndex }, -1, -1, edge.IsFlippedOnFace(face), null);
              speckleEdges.Add(edge, sEdge);
              speckleEdgeIndexes.Add(edge, edgeIndex);
              edgeIndex++;
            }
            else
            {
              // Already visited this edge, skip curve 3d
              var sEdge = speckleEdges[edge];
              var sEdgeIndex = speckleEdgeIndexes[edge];
              sTrim.EdgeIndex = sEdgeIndex;

              // Update trim indices with new item.
              // TODO: Make this better.
              var trimIndices = sEdge.TrimIndices.ToList();
              trimIndices.Append(sTrimIndex); //TODO Append is a pure function and the return is unused
              sEdge.TrimIndices = trimIndices.ToArray();
            }
          }

          var speckleLoop = new BrepLoop(brep, faceIndex, loopTrimIndices, BrepLoopType.Outer);
          speckleLoops.Add(speckleLoop);
          var sLoopIndex = loopIndex;
          loopIndex++;
          loopIndices.Add(sLoopIndex);
        }

        speckleFaces.Add(face,
          new BrepFace(brep, surfaceIndex, loopIndices, loopIndices[0], !face.OrientationMatchesSurfaceOrientation));
        faceIndex++;
        brep.Surfaces.Add(surface);
        surfaceIndex++;
      }

      // TODO: Revit has no brep vertices. Must call 'brep.SetVertices()' in rhino when provenance is revit.
      // TODO: Set tolerances and flags in rhino when provenance is revit.
      brep.Faces = speckleFaces.Values.ToList();
      brep.Curve2D = speckle2dCurves;
      brep.Curve3D = speckle3dCurves.ToList();
      brep.Trims = speckleTrims;
      brep.Edges = speckleEdges.Values.ToList();
      brep.Loops = speckleLoops;
      brep.displayValue = GetMeshesFromSolids(new[] { solid }, d);
      return brep;

#endif
    }

    public Surface FaceToSpeckle(DB.Face face, out bool parametricOrientation, double relativeTolerance = 0.0, string units = null)
    {
      var u = units ?? ModelUnits;
      using (var surface = face.GetSurface())
        parametricOrientation = surface.OrientationMatchesParametricOrientation;

      switch (face)
      {
        case null:
          return null;
        case PlanarFace planar:
          return FaceToSpeckle(planar, relativeTolerance, u);
        case ConicalFace conical:
          return FaceToSpeckle(conical, relativeTolerance, u);
        case CylindricalFace cylindrical:
          return FaceToSpeckle(cylindrical, relativeTolerance, u);
        case RevolvedFace revolved:
          return FaceToSpeckle(revolved, relativeTolerance, u);
        case RuledFace ruled:
          return FaceToSpeckle(ruled, relativeTolerance, u);
        case HermiteFace hermite:
          return FaceToSpeckle(hermite, face.GetBoundingBox(), u);
        default:
          throw new NotImplementedException();
      }
    }
    public Surface FaceToSpeckle(PlanarFace planarFace, double tolerance, string units = null)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(ConicalFace conicalFace, double tolerance, string units = null)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(CylindricalFace cylindricalFace, double tolerance, string units = null)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(RevolvedFace revolvedFace, double tolerance, string units = null)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(RuledFace ruledFace, double tolerance, string units = null)
    {
      throw new NotImplementedException();
    }

    public int AddSurface(Brep brep, DB.Face face, out List<BrepBoundary>[] shells,
      Dictionary<DB.Edge, BrepEdge> brepEdges = null)
    {
      throw new NotImplementedException();
    }

    public void TrimSurface(Brep brep, int surface, bool orientationReversed, List<BrepBoundary>[] shells)
    {
      // TODO: Incomplete method.
      foreach (var shell in shells)
      {
        //var sFace = new BrepFace(brep,surface,null,null,orientationReversed);

        foreach (var loop in shell)
        {
          var brepLoop = 0;
          var edgeCount = loop.edges.Count;

          for (int e = 0; e < edgeCount; ++e)
          {
            var brepEdge = loop.edges[e];
            var orientation = loop.orientation[e];
            if (orientation == 0) continue;

            if (loop.trims.segments[e] is Curve trim)
            {
              brep.Curve2D.Add(trim);
              // TODO: Missing stuff here!
            }
          }
        }

      }
      throw new NotImplementedException();
    }

    public struct BrepBoundary
    {
      public BrepLoopType type;
      public List<BrepEdge> edges;
      public Polycurve trims;
      public List<int> orientation;
    }

    public DirectShape BrepToDirectShape(Brep brep, BuiltInCategory cat = BuiltInCategory.OST_GenericModel)
    {
      var revitDs = DirectShape.CreateElement(Doc, new ElementId(cat));

      try
      {
        var solid = BrepToNative(brep);
        if (solid == null) throw new Speckle.Core.Logging.SpeckleException("Could not convert Brep to Solid");
        revitDs.SetShape(new List<GeometryObject> { solid });
      }
      catch (Exception e)
      {
        Report.LogConversionError(new Exception($"Failed to convert BREP with id {brep.id}, using display mesh value instead.", e));
        var meshes = brep.displayValue.SelectMany(m => MeshToNative(m));
        revitDs.SetShape(meshes.ToArray());
      }
      return revitDs;
    }
  }
}

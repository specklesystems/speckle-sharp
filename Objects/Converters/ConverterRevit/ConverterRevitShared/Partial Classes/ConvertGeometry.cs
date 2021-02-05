using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using Curve = Objects.Geometry.Curve;
using DB = Autodesk.Revit.DB;
using Ellipse = Objects.Geometry.Ellipse;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Surface = Objects.Geometry.Surface;

namespace Objects.Converter.Revit
{
  /// <summary>
  ///Internal helper methods used for converison
  /// </summary>
  public partial class ConverterRevit
  {
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
          throw new Exception("Cannot convert Curve of type " + geom.GetType());
      }
    }

    public XYZ PointToNative(Point pt)
    {
      var revitPoint = new XYZ(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units));
      var intPt = ToInternalCoordinates(revitPoint);
      return intPt;
    }

    public Point PointToSpeckle(XYZ pt)
    {
      var extPt = ToExternalCoordinates(pt);
      var pointToSpeckle = new Point(ScaleToSpeckle(extPt.X), ScaleToSpeckle(extPt.Y), ScaleToSpeckle(extPt.Z), ModelUnits);
      return pointToSpeckle;
    }

    public XYZ VectorToNative(Vector pt)
    {
      var revitVector = new XYZ(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units));
      var intV = ToInternalCoordinates(revitVector);
      return intV;
    }

    public DB.Plane PlaneToNative(Plane plane)
    {
      return DB.Plane.CreateByOriginAndBasis(PointToNative(plane.origin), VectorToNative(plane.xdir).Normalize(), VectorToNative(plane.ydir).Normalize());
    }

    public Plane PlaneToSpeckle(DB.Plane plane)
    {
      var origin = PointToSpeckle(plane.Origin);
      var normal = new Vector(ScaleToSpeckle(plane.Normal.X), ScaleToSpeckle(plane.Normal.Y), ScaleToSpeckle(plane.Normal.Z), ModelUnits);
      var xdir = new Vector(ScaleToSpeckle(plane.XVec.X), ScaleToSpeckle(plane.XVec.Y), ScaleToSpeckle(plane.XVec.Z), ModelUnits);
      var ydir = new Vector(ScaleToSpeckle(plane.YVec.X), ScaleToSpeckle(plane.YVec.Y), ScaleToSpeckle(plane.YVec.Z), ModelUnits);

      return new Plane(origin, normal, xdir, ydir, ModelUnits);
    }

    public DB.Line LineToNative(Line line)
    {
      return DB.Line.CreateBound(
        PointToNative(line.start),
        PointToNative(line.end));
    }

    public Line LineToSpeckle(DB.Line line)
    {
      var l = new Line { units = ModelUnits };
      l.start = PointToSpeckle(line.GetEndPoint(0));
      l.end = PointToSpeckle(line.GetEndPoint(1));
      
      l.length = line.Length;
      return l;
    }

    public Circle CircleToSpeckle(DB.Arc arc)
    {
      // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
      var arcPlane = DB.Plane.CreateByNormalAndOrigin(arc.Normal, arc.Center);

      var c = new Circle(PlaneToSpeckle(arcPlane), ScaleToSpeckle(arc.Radius), ModelUnits);
      c.length = arc.Length;
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
      return DB.Arc.Create(PointToNative(arc.startPoint), PointToNative(arc.endPoint), PointToNative(arc.midPoint));
      //return Arc.Create( plane.Origin, (double) arc.Radius * Scale, startAngle, endAngle, plane.XVec, plane.YVec );
    }

    public Arc ArcToSpeckle(DB.Arc arc)
    {

      // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
      var arcPlane = DB.Plane.CreateByNormalAndOrigin(arc.Normal, arc.Center);

      XYZ center = arc.Center;


      XYZ dir0 = (arc.GetEndPoint(0) - center).Normalize();
      XYZ dir1 = (arc.GetEndPoint(1) - center).Normalize();

      XYZ start = arc.Evaluate(0, true);
      XYZ end = arc.Evaluate(1, true);
      XYZ mid = arc.Evaluate(0.5, true);

      double startAngle = dir0.AngleOnPlaneTo(arc.XDirection, arc.Normal);
      double endAngle = dir1.AngleOnPlaneTo(arc.XDirection, arc.Normal);

      var a = new Arc(PlaneToSpeckle(arcPlane), ScaleToSpeckle(arc.Radius), startAngle, endAngle, endAngle - startAngle, ModelUnits);
      a.endPoint = PointToSpeckle(end);
      a.startPoint = PointToSpeckle(start);
      a.midPoint = PointToSpeckle(mid);
      a.length = arc.Length;
      
      return a;
    }

    public DB.Ellipse EllipseToNative(Ellipse ellipse)
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
           ellipse.domain.start ?? 0,
           ellipse.domain.end ?? 2 * Math.PI
          ) as DB.Ellipse;

        e.MakeBound(ellipse.trimDomain?.start ?? 0, ellipse.trimDomain?.end ?? 2 * Math.PI);
        return e;
      }
    }

    public Ellipse EllipseToSpeckle(DB.Ellipse ellipse)
    {
      using (DB.Plane basePlane = DB.Plane.CreateByOriginAndBasis(ellipse.Center, ellipse.XDirection, ellipse.YDirection))
      {
        var trim = ellipse.IsBound ? new Interval(ellipse.GetEndParameter(0), ellipse.GetEndParameter(1)) : null;

        var ellipseToSpeckle = new Ellipse(
          PlaneToSpeckle(basePlane),
          ScaleToSpeckle(ellipse.RadiusX),
          ScaleToSpeckle(ellipse.RadiusY),
          new Interval(0, 2 * Math.PI),
          trim,
          ModelUnits);
        ellipseToSpeckle.length = ellipse.Length;
        return ellipseToSpeckle;
      }
    }

    public Curve NurbsToSpeckle(DB.NurbSpline revitCurve)
    {
      var points = new List<double>();
      foreach (var p in revitCurve.CtrlPoints)
      {
        points.AddRange(new List<double>{ScaleToSpeckle(p.X),ScaleToSpeckle(p.Y),ScaleToSpeckle(p.Z)});
      }

      Curve speckleCurve = new Curve();
      speckleCurve.weights = revitCurve.Weights.Cast<double>().ToList();
      speckleCurve.points = points;
      speckleCurve.knots = revitCurve.Knots.Cast<double>().ToList(); ;
      speckleCurve.degree = revitCurve.Degree;
      //speckleCurve.periodic = revitCurve.Period;
      speckleCurve.rational = revitCurve.isRational;
      speckleCurve.closed = RevitVersionHelper.IsCurveClosed(revitCurve);
      speckleCurve.units = ModelUnits;
      //speckleCurve.domain = new Interval(revitCurve.StartParameter(), revitCurve.EndParameter());
      speckleCurve.length = revitCurve.Length;
      
      return speckleCurve;
    }


    public DB.Curve CurveToNative(Curve speckleCurve)
    {
      var pts = new List<XYZ>();
      for (int i = 0; i < speckleCurve.points.Count; i += 3)
      {
        pts.Add(new XYZ(
          ScaleToNative(speckleCurve.points[i], speckleCurve.units),
          ScaleToNative(speckleCurve.points[i + 1], speckleCurve.units),
          ScaleToNative(speckleCurve.points[i + 2], speckleCurve.units)));
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
        return null;
      }
    }

    /// <summary>
    /// Recursively creates an ordered list of curves from a polycurve/polyline.
    /// Please note that a polyline is broken down into lines.
    /// </summary>
    /// <param name="crv">A speckle curve.</param>
    /// <returns></returns>
    public CurveArray CurveToNative(ICurve crv)
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

        case Curve nurbs:
          curveArray.Append(CurveToNative(nurbs));
          return curveArray;

        case Polyline poly:
          return PolylineToNative(poly);

        case Polycurve plc:
          foreach (var seg in plc.segments)
          {
            curveArray.Append(CurveToNative(seg).get_Item(0));
          }

          return curveArray;
        default:
          throw new Exception("The provided geometry is not a valid curve");
      }
    }

    //thanks Revit
    public CurveLoop CurveArrayToCurveLoop(CurveArray array)
    {
      var loop = new CurveLoop();
      foreach (var item in array.Cast<DB.Curve>())
        loop.Append(item);

      return loop;
    }

    public ICurve CurveToSpeckle(DB.Curve curve)
    {
      switch (curve)
      {
        case DB.Line line:
          return LineToSpeckle(line);
        case DB.Arc arc:
          if (!arc.IsBound)
          {
            return (CircleToSpeckle(arc));
          }
          return ArcToSpeckle(arc);
        case DB.Ellipse ellipse:
          return EllipseToSpeckle(ellipse);
        case DB.NurbSpline nurbs:
          return NurbsToSpeckle(nurbs);
        case DB.HermiteSpline spline:
          return HermiteSplineToSpeckle(spline);
        default:
          throw new Exception("Cannot convert Curve of type " + curve.GetType());
      }
    }

    public Polycurve CurveListToSpeckle(IList<DB.Curve> loop)
    {
      var polycurve = new Polycurve();
      polycurve.units = ModelUnits;
      polycurve.closed = loop.First().GetEndPoint(0).DistanceTo(loop.Last().GetEndPoint(1)) < 0.0164042; //5mm
      polycurve.length = loop.Sum(x => x.Length);
      polycurve.segments.AddRange(loop.Select(x => CurveToSpeckle(x)));
      return polycurve;
    }

    public Polycurve CurveLoopToSpeckle(CurveLoop loop)
    {
      var polycurve = new Polycurve();
      polycurve.units = ModelUnits;
      polycurve.closed = !loop.IsOpen();
      polycurve.length = ScaleToSpeckle(loop.GetExactLength());

      polycurve.segments.AddRange(loop.Select(x => CurveToSpeckle(x)));
      return polycurve;
    }

    private ICurve HermiteSplineToSpeckle(HermiteSpline spline)
    {
      var nurbs = DB.NurbSpline.Create(spline);
      return NurbsToSpeckle(nurbs);
    }

    public CurveArray PolylineToNative(Polyline polyline)
    {
      var curveArray = new CurveArray();
      if (polyline.value.Count == 6)
      {
        curveArray.Append(LineToNative(new Line(polyline.value, polyline.units)));
      }
      else
      {
        var pts = polyline.points;

        for (var i = 1; i < pts.Count; i++)
        {
          var speckleLine = new Line(new double[] { pts[i - 1].x, pts[i - 1].y, pts[i - 1].z, pts[i].x, pts[i].y, pts[i].z }, polyline.units);
          curveArray.Append(LineToNative(speckleLine));
        }

        if (polyline.closed)
        {
          var speckleLine = new Line(new double[] { pts[pts.Count - 1].x, pts[pts.Count - 1].y, pts[pts.Count - 1].z, pts[0].x, pts[0].y, pts[0].z }, polyline.units);
          curveArray.Append(LineToNative(speckleLine));
        }
      }
      return curveArray;
    }

    public Mesh MeshToSpeckle(DB.Mesh mesh)
    {
      var speckleMesh = new Mesh();
      foreach (var vert in mesh.Vertices)
      {
        speckleMesh.vertices.AddRange(new double[] { ScaleToSpeckle(vert.X), ScaleToSpeckle(vert.Y), ScaleToSpeckle(vert.Z) });
      }

      for (int i = 0; i < mesh.NumTriangles; i++)
      {
        var triangle = mesh.get_Triangle(i);
        var A = triangle.get_Index(0);
        var B = triangle.get_Index(1);
        var C = triangle.get_Index(2);
        speckleMesh.faces.Add(0);
        speckleMesh.faces.AddRange(new int[] { (int)A, (int)B, (int)C });
      }

      speckleMesh.units = ModelUnits;
      
      return speckleMesh;
    }

    // Insipred by
    // https://github.com/DynamoDS/DynamoRevit/blob/master/src/Libraries/RevitNodes/GeometryConversion/ProtoToRevitMesh.cs
    public IList<GeometryObject> MeshToNative(Mesh mesh)
    {

      TessellatedShapeBuilderTarget target = TessellatedShapeBuilderTarget.Mesh;
      TessellatedShapeBuilderFallback fallback = TessellatedShapeBuilderFallback.Salvage;

      var tsb = new TessellatedShapeBuilder() { Fallback = fallback, Target = target, GraphicsStyleId = ElementId.InvalidElementId };
      tsb.OpenConnectedFaceSet(false);

      var vertices = ArrayToPoints(mesh.vertices, mesh.units);

      int i = 0;

      while (i < mesh.faces.Count)
      {
        var points = new List<XYZ>();

        if (mesh.faces[i] == 0)
        { // triangle
          points = new List<XYZ> { vertices[mesh.faces[i + 1]], vertices[mesh.faces[i + 2]], vertices[mesh.faces[i + 3]] };
          i += 4;
        }
        else
        { // quad
          points = new List<XYZ> { vertices[mesh.faces[i + 1]], vertices[mesh.faces[i + 2]], vertices[mesh.faces[i + 3]], vertices[mesh.faces[i + 4]] };
          i += 5;
        }

        var face = new TessellatedFace(points, ElementId.InvalidElementId);
        tsb.AddFace(face);
      }

      tsb.CloseConnectedFaceSet();
      tsb.Build();
      var result = tsb.GetBuildResult();
      return result.GetGeometricalObjects();


    }

    public XYZ[] ArrayToPoints(IEnumerable<double> arr, string units)
    {
      if (arr.Count() % 3 != 0)
      {
        throw new Exception("Array malformed: length%3 != 0.");
      }

      XYZ[] points = new XYZ[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
      {
        var p = new XYZ(ScaleToNative(asArray[i - 2], units), ScaleToNative(asArray[i - 1], units), ScaleToNative(asArray[i], units));
        var intP = ToInternalCoordinates(p);
        points[k++] = intP;
      }

      return points;
    }

    //https://github.com/DynamoDS/DynamoRevit/blob/f8206726d8a3aa5bf06f5dbf7ce8a732bb025c34/src/Libraries/RevitNodes/GeometryConversion/GeometryPrimitiveConverter.cs#L201
    public XYZ GetPerpendicular(XYZ xyz)
    {
      var ixn = xyz.Normalize();
      var xn = new XYZ(1, 0, 0);

      if (ixn.IsAlmostEqualTo(xn))
      {
        xn = new XYZ(0, 1, 0);
      }

      return ixn.CrossProduct(xn).Normalize();
    }

    public Geometry.Surface FaceToSpeckle(DB.Face face, DB.BoundingBoxUV uvBox)
    {

#if REVIT2021
      var surf = DB.ExportUtils.GetNurbsSurfaceDataForSurface(face.GetSurface());
#else
      var surf = DB.ExportUtils.GetNurbsSurfaceDataForFace(face);
#endif
      var spcklSurface = NurbsSurfaceToSpeckle(surf, face.GetBoundingBox());
      return spcklSurface;
    }

    public Surface NurbsSurfaceToSpeckle(DB.NurbsSurfaceData surface, DB.BoundingBoxUV uvBox)
    {
      var result = new Surface();

      result.units = ModelUnits;

      result.degreeU = surface.DegreeU;
      result.degreeV = surface.DegreeV;

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
          var extPt = ToExternalCoordinates(pt);
          if (surface.IsRational)
          {
            var w = weights[uOffset + v];
            row.Add(new ControlPoint(ScaleToSpeckle(extPt.X), ScaleToSpeckle(extPt.Y), ScaleToSpeckle(extPt.Z), w, ModelUnits));
          }
          else
          {
            row.Add(new ControlPoint(ScaleToSpeckle(extPt.X), ScaleToSpeckle(extPt.Y), ScaleToSpeckle(extPt.Z), ModelUnits));
          }
        }
        points.Add(row);
      }
      
      result.SetControlPoints(points);
      
      return result;
    }

    public Face BrepFaceToNative(BrepFace face)
    {
      var brep = BrepToNative(face.Brep);
      var faceIndex = face.SurfaceIndex;
      return brep.Faces.get_Item(faceIndex);
    }

    public List<BRepBuilderEdgeGeometry> BrepEdgeToNative(BrepEdge edge)
    {
      // TODO: Trim curve with domain. Unsure if this is necessary as all our curves are converted to NURBS on Rhino output.

      var nativeCurveArray = CurveToNative(edge.Curve);
      bool isTrimmed = edge.Curve.domain != null && edge.Domain != null &&
                       (edge.Curve.domain.start != edge.Domain.start
                        || edge.Curve.domain.end != edge.Domain.end);
      if (nativeCurveArray.Size == 1)
      {
        var nativeCurve = nativeCurveArray.get_Item(0);

        if (edge.ProxyCurveIsReversed)
          nativeCurve = nativeCurve.CreateReversed();

        if (nativeCurve == null)
          return new List<BRepBuilderEdgeGeometry>();
        if (isTrimmed)
        {
          nativeCurve.MakeBound(edge.Domain.start ?? 0, edge.Domain.end ?? 1);
        }
        if (!nativeCurve.IsBound)
          nativeCurve.MakeBound(0, nativeCurve.Period);

        var endPoint = nativeCurve.GetEndPoint(0);
        var source = nativeCurve.GetEndPoint(1);
        var distanceTo = endPoint.DistanceTo(source);
        var closed = distanceTo < 1E-6;
        if (closed)
        {
          // Revit does not like single curve loop edges, so we split them in two.
          var start = nativeCurve.GetEndParameter(0);
          var end = nativeCurve.GetEndParameter(1);
          var mid = (end - start) / 2;

          var a = nativeCurve.Clone();
          a.MakeBound(start, mid);

          var b = nativeCurve.Clone();
          b.MakeBound(mid, end);

          var halfEdgeA = BRepBuilderEdgeGeometry.Create(a);
          var halfEdgeB = BRepBuilderEdgeGeometry.Create(b);
          return new List<BRepBuilderEdgeGeometry> { halfEdgeA, halfEdgeB };
        }

        // TODO: Remove short segments if smaller than 'Revit.ShortCurveTolerance'.
        var fullEdge = BRepBuilderEdgeGeometry.Create(nativeCurve);
        return new List<BRepBuilderEdgeGeometry> { fullEdge };
      }

      var iterator = edge.ProxyCurveIsReversed
        ? nativeCurveArray.ReverseIterator()
        : nativeCurveArray.ForwardIterator();

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
          var revitPt = new DB.XYZ(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units));
          var intPt = ToInternalCoordinates(revitPt);
          points[p++] = intPt;
        }
         ));

      return points;
    }

    public double[] SurfaceKnotsToNative(List<double> list)
    {
      var count = list.Count;
      var knots = new double[count + 2];

      int j = 0, k = 0;
      while (j < count)
      {
        knots[++k] = list[j++];
      }

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

      using var builder = new BRepBuilder(bRepType);

      builder.SetAllowShortEdges();
      builder.AllowRemovalOfProblematicFaces();

      var brepEdges = new List<DB.BRepBuilderGeometryId>[brep.Edges.Count];
      foreach (var face in brep.Faces)
      {
        var faceId = builder.AddFace(SurfaceToNative(face.Surface), face.OrientationReversed);

        foreach (var loop in face.Loops)
        {
          var loopId = builder.AddLoop(faceId);
          if (face.OrientationReversed)
            loop.TrimIndices.Reverse();

          foreach (var trim in loop.Trims)
          {
            if (trim.TrimType != BrepTrimType.Boundary && trim.TrimType != BrepTrimType.Mated)
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

    public Brep BrepToSpeckle(Solid solid)
    {
#if REVIT2021
      // TODO: Incomplete implementation!!

      var brep = new Brep();
      brep.units = ModelUnits;

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
            speckle2dCurves.Add(CurveToSpeckle(trim.As3DCurveInXYPlane()));
            speckleTrims.Add(sTrim);
            curve2dIndex++;
            trimIndex++;

            // Check if we have visited this edge before.
            if (!speckleEdges.ContainsKey(edge))
            {
              // First time we visit this edge, add 3d curve and create new BrepEdge.
              var edgeCurve = edge.AsCurve();
              speckle3dCurves[curve3dIndex] = CurveToSpeckle(edgeCurve);
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
              trimIndices.Append(sTrimIndex);
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

      var mesh = new Mesh();
      (mesh.faces, mesh.vertices) = GetFaceVertexArrFromSolids(new List<Solid> { solid });
      mesh.units = ModelUnits;
      // TODO: Revit has no brep vertices. Must call 'brep.SetVertices()' in rhino when provenance is revit.
      // TODO: Set tolerances and flags in rhino when provenance is revit.
      brep.Faces = speckleFaces.Values.ToList();
      brep.Curve2D = speckle2dCurves;
      brep.Curve3D = speckle3dCurves.ToList();
      brep.Trims = speckleTrims;
      brep.Edges = speckleEdges.Values.ToList();
      brep.Loops = speckleLoops;
      brep.displayValue = mesh;
      return brep;
#else
      throw new Exception("Converting BREPs to Speckle is currently only supported in Revit 2021.");
#endif
    }

    public Surface FaceToSpeckle(DB.Face face, out bool parametricOrientation, double relativeTolerance = 0.0)
    {
      using (var surface = face.GetSurface())
        parametricOrientation = surface.OrientationMatchesParametricOrientation;

      switch (face)
      {
        case null: return null;
        case PlanarFace planar: return FaceToSpeckle(planar, relativeTolerance);
        case ConicalFace conical: return FaceToSpeckle(conical, relativeTolerance);
        case CylindricalFace cylindrical: return FaceToSpeckle(cylindrical, relativeTolerance);
        case RevolvedFace revolved: return FaceToSpeckle(revolved, relativeTolerance);
        case RuledFace ruled: return FaceToSpeckle(ruled, relativeTolerance);
        case HermiteFace hermite: return FaceToSpeckle(hermite, face.GetBoundingBox());
        default: throw new NotImplementedException();
      }
    }
    public Surface FaceToSpeckle(PlanarFace planarFace, double tolerance)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(ConicalFace conicalFace, double tolerance)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(CylindricalFace cylindricalFace, double tolerance)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(RevolvedFace revolvedFace, double tolerance)
    {
      throw new NotImplementedException();
    }
    public Surface FaceToSpeckle(RuledFace ruledFace, double tolerance)
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
        if (solid == null) throw new Exception("Could not convert Brep to Solid");
        revitDs.SetShape(new List<GeometryObject> { solid });
      }
      catch (Exception e)
      {
        var mesh = MeshToNative(brep.displayValue);
        revitDs.SetShape(mesh);
        ConversionErrors.Add(new Error(e.Message, e.InnerException?.Message ?? "No details available."));
      }
      return revitDs;
    }
  }
}

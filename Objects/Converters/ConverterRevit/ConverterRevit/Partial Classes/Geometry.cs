using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Point = Objects.Geometry.Point;
using Plane = Objects.Geometry.Plane;
using Line = Objects.Geometry.Line;
using Arc = Objects.Geometry.Arc;
using Curve = Objects.Geometry.Curve;
using Mesh = Objects.Geometry.Mesh;
using System.Linq;
using Ellipse = Objects.Geometry.Ellipse;
using Objects;

namespace Objects.Converter.Revit
{
  /// <summary>
  ///Internal helper methods used for converison
  /// </summary>
  public partial class ConverterRevit
  {
    public object GeometryToNative(IGeometry geom)
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
      return new XYZ(ScaleToNative(pt.value[0], pt.units), ScaleToNative(pt.value[1], pt.units), ScaleToNative(pt.value[2], pt.units));
    }

    public Point PointToSpeckle(XYZ pt)
    {
      return new Point(ScaleToSpeckle(pt.X), ScaleToSpeckle(pt.Y), ScaleToSpeckle(pt.Z), ModelUnits);
    }

    public XYZ VectorToNative(Vector pt)
    {
      return new XYZ(ScaleToNative(pt.value[0], pt.units), ScaleToNative(pt.value[1], pt.units), ScaleToNative(pt.value[2], pt.units));
    }

    public DB.Plane PlaneToNative(Plane plane)
    {
      return DB.Plane.CreateByOriginAndBasis(PointToNative(plane.origin), VectorToNative(plane.xdir).Normalize(), VectorToNative(plane.ydir).Normalize());
    }

    public Plane PlaneToSpeckle(DB.Plane plane)
    {
      var origin = PointToSpeckle(plane.Origin);
      var normal = new Vector(ScaleToSpeckle(plane.Normal.X), ScaleToSpeckle(plane.Normal.Y), ScaleToSpeckle(plane.Normal.Z));
      var xdir = new Vector(ScaleToSpeckle(plane.XVec.X), ScaleToSpeckle(plane.XVec.Y), ScaleToSpeckle(plane.XVec.Z));
      var ydir = new Vector(ScaleToSpeckle(plane.YVec.X), ScaleToSpeckle(plane.YVec.Y), ScaleToSpeckle(plane.YVec.Z));

      return new Plane(origin, normal, xdir, ydir);
    }

    public DB.Line LineToNative(Line line)
    {
      return DB.Line.CreateBound(new XYZ(line.value[0] * Scale, line.value[1] * Scale, line.value[2] * Scale), new XYZ(line.value[3] * Scale, line.value[4] * Scale, line.value[5] * Scale));
    }

    public Line LineToSpeckle(DB.Line line)
    {
      var l = new Line() { value = new List<double>() };
      l.value.AddRange(PointToSpeckle(line.GetEndPoint(0)).value);
      l.value.AddRange(PointToSpeckle(line.GetEndPoint(1)).value);
      return l;
    }

    public Circle CircleToSpeckle(DB.Arc arc)
    {
      // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
      var arcPlane = DB.Plane.CreateByNormalAndOrigin(arc.Normal, arc.Center);

      var c = new Circle(PlaneToSpeckle(arcPlane), arc.Radius / Scale);
      return c;
    }

    public DB.Arc CircleToNative(Circle circle)
    {
      var plane = PlaneToNative(circle.plane);
      return DB.Arc.Create(plane, (double)circle.radius * Scale, 0, 2 * Math.PI);
    }

    public DB.Arc ArcToNative(Arc arc)
    {
      double startAngle, endAngle;
      if (arc.startAngle > arc.endAngle) { startAngle = (double)arc.endAngle; endAngle = (double)arc.startAngle; }
      else { startAngle = (double)arc.startAngle; endAngle = (double)arc.endAngle; }
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

      var a = new Arc(PlaneToSpeckle(arcPlane), arc.Radius / Scale, startAngle, endAngle, endAngle - startAngle);
      a.endPoint = PointToSpeckle(end);
      a.startPoint = PointToSpeckle(start);
      a.midPoint = PointToSpeckle(mid);
      return a;
    }

    public DB.Ellipse EllipseToNative(Ellipse ellipse)
    {
      //TODO: support ellipse arcs
      using (DB.Plane basePlane = PlaneToNative(ellipse.plane))
      {
        var e = DB.Ellipse.CreateCurve(
          PointToNative(ellipse.plane.origin),
          (double)ellipse.firstRadius * Scale,
          (double)ellipse.secondRadius * Scale,
          basePlane.XVec.Normalize(),
          basePlane.YVec.Normalize(),
          0,
           2 * Math.PI
          ) as DB.Ellipse;
        e.MakeBound(0, 2 * Math.PI);
        return e;
      }
    }

    public Ellipse EllipseToSpeckle(DB.Ellipse ellipse)
    {
      using (DB.Plane basePlane = DB.Plane.CreateByOriginAndBasis(ellipse.Center, ellipse.XDirection, ellipse.YDirection))
      {
        return new Ellipse(
          PlaneToSpeckle(basePlane),
          ellipse.RadiusX / Scale,
          ellipse.RadiusY / Scale);
      }
    }

    public Curve NurbsToSpeckle(DB.NurbSpline revitCurve)
    {
      var points = new List<double>();
      foreach (var p in revitCurve.CtrlPoints)
      {
        points.AddRange(PointToSpeckle(p).value);
      }


      Curve speckleCurve = new Curve();
      speckleCurve.weights = revitCurve.Weights.Cast<double>().ToList();
      speckleCurve.points = points;
      speckleCurve.knots = revitCurve.Knots.Cast<double>().ToList(); ;
      speckleCurve.degree = revitCurve.Degree;
      //speckleCurve.periodic = revitCurve.Period;
      speckleCurve.rational = revitCurve.isRational;
      speckleCurve.closed = revitCurve.IsClosed;
      //speckleCurve.domain = new Interval(revitCurve.StartParameter(), revitCurve.EndParameter());

      return speckleCurve;
    }


    public DB.Curve CurveToNative(Curve speckleCurve)
    {
      var pts = new List<XYZ>();
      for (int i = 0; i < speckleCurve.points.Count; i += 3)
      {
        pts.Add(new XYZ(speckleCurve.points[i] * Scale, speckleCurve.points[i + 1] * Scale, speckleCurve.points[i + 2] * Scale));
      }
      try
      {
        if (speckleCurve.knots != null && speckleCurve.weights != null && speckleCurve.knots.Any() && speckleCurve.weights.Any())
        {
          var weights = speckleCurve.weights.GetRange(0, pts.Count);
          var knots = speckleCurve.knots.GetRange(0, pts.Count + speckleCurve.degree + 1);
          var curve = NurbSpline.CreateCurve(speckleCurve.degree, knots, pts, weights);
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
            curveArray.Append(CurveToNative(seg).get_Item(0));
          return curveArray;
        default:
          throw new Exception("The provided geometry is not a valid curve");
      }
    }

    public ICurve CurveToSpeckle(DB.Curve curve)
    {
      switch (curve)
      {
        case DB.Line line:
          return LineToSpeckle(line);
        case DB.Arc arc:
          if (!arc.IsBound)
            return (CircleToSpeckle(arc));
          return ArcToSpeckle(arc);
        case DB.Ellipse ellipse:
          return EllipseToSpeckle(ellipse);
        case DB.NurbSpline nurbs:
          return NurbsToSpeckle(nurbs);
        default:
          throw new Exception("Cannot convert Curve of type " + curve.GetType());
      }
    }

    public CurveArray PolylineToNative(Polyline polyline)
    {
      var curveArray = new CurveArray();
      if (polyline.value.Count == 6)
      {
        curveArray.Append(LineToNative(new Line(polyline.value)));
      }
      else
      {
        List<Point> pts = polyline.points;

        for (int i = 1; i < pts.Count; i++)
        {
          var speckleLine = new Line(new double[] { pts[i - 1].value[0], pts[i - 1].value[1], pts[i - 1].value[2], pts[i].value[0], pts[i].value[1], pts[i].value[2] });

          curveArray.Append(LineToNative(speckleLine));
        }

        if (polyline.closed)
        {
          var speckleLine = new Line(new double[] { pts[pts.Count - 1].value[0], pts[pts.Count - 1].value[1], pts[pts.Count - 1].value[2], pts[0].value[0], pts[0].value[1], pts[0].value[2] });
          curveArray.Append(LineToNative(speckleLine));
        }
      }
      return curveArray;
    }

    // Insipred by
    // https://github.com/DynamoDS/DynamoRevit/blob/master/src/Libraries/RevitNodes/GeometryConversion/ProtoToRevitMesh.cs
    public IList<GeometryObject> MeshToNative(Mesh mesh)
    {

      TessellatedShapeBuilderTarget target = TessellatedShapeBuilderTarget.Mesh;
      TessellatedShapeBuilderFallback fallback = TessellatedShapeBuilderFallback.Salvage;

      var tsb = new TessellatedShapeBuilder() { Fallback = fallback, Target = target, GraphicsStyleId = ElementId.InvalidElementId };
      tsb.OpenConnectedFaceSet(false);

      var vertices = ArrayToPoints(mesh.vertices);

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

    public XYZ[] ArrayToPoints(IEnumerable<double> arr)
    {
      if (arr.Count() % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      XYZ[] points = new XYZ[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
        points[k++] = new XYZ(asArray[i - 2] * Scale, asArray[i - 1] * Scale, asArray[i] * Scale);

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
  }
}

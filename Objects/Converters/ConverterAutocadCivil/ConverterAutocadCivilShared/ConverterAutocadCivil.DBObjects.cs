using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadBRep = Autodesk.AutoCAD.BoundaryRepresentation;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
using System.Drawing;

using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;
using Brep = Objects.Geometry.Brep;
using BrepEdge = Objects.Geometry.BrepEdge;
using BrepFace = Objects.Geometry.BrepFace;
using BrepLoop = Objects.Geometry.BrepLoop;
using BrepLoopType = Objects.Geometry.BrepLoopType;
using BrepTrim = Objects.Geometry.BrepTrim;
using Circle = Objects.Geometry.Circle;
using ControlPoint = Objects.Geometry.ControlPoint;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Hatch = Objects.Other.Hatch;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Surface = Objects.Geometry.Surface;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Text = Objects.Other.Text;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Autodesk.AutoCAD.Windows.Data;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // Points
    public Point PointToSpeckle(DBPoint point, string units = null)
    {
      var u = units ?? ModelUnits;
      return PointToSpeckle(point.Position, u);
    }
    public DBPoint PointToNativeDB(Point point)
    {
      return new DBPoint(PointToNative(point));
    }
    public List<List<ControlPoint>> ControlPointsToSpeckle(AcadDB.NurbSurface surface)
    {
      var points = new List<List<ControlPoint>>();
      for (var i = 0; i < surface.NumberOfControlPointsInU; i++)
      {
        var row = new List<ControlPoint>();
        for (var j = 0; j < surface.NumberOfControlPointsInV; j++)
        {
          var point = surface.GetControlPointAt(i, j);
          var weight = surface.GetWeight(i, j);
          row.Add(new ControlPoint(point.X, point.Y, point.Z, weight, ModelUnits));
        }
        points.Add(row);
      }
      return points;
    }

    // Lines
    public Line LineToSpeckle(AcadDB.Line line, string units = null)
    {
      var u = units ?? ModelUnits;

      var _line = new Line(PointToSpeckle(line.StartPoint, u), PointToSpeckle(line.EndPoint, u), u);
      _line.domain = new Interval(line.StartParam, line.EndParam);
      _line.length = line.Length;
      _line.bbox = BoxToSpeckle(line.GeometricExtents, true);
      return _line;
    }
    public AcadDB.Line LineToNativeDB(Line line)
    {
      return new AcadDB.Line(PointToNative(line.start), PointToNative(line.end));
    }

    // Boxes
    public Box BoxToSpeckle(AcadDB.Extents3d extents, bool OrientToWorldXY = false)
    {
      try
      {
        Box box = null;

        // get dimension intervals
        var xSize = new Interval(extents.MinPoint.X, extents.MaxPoint.X);
        var ySize = new Interval(extents.MinPoint.Y, extents.MaxPoint.Y);
        var zSize = new Interval(extents.MinPoint.Z, extents.MaxPoint.Z);

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
          var corner = new Point3d(extents.MaxPoint.X, extents.MaxPoint.Y, extents.MinPoint.Z);
          var origin = new Point3d((corner.X + extents.MinPoint.X) / 2, (corner.Y + extents.MinPoint.Y) / 2, (corner.Z + extents.MinPoint.Z) / 2);
          var plane = PlaneToSpeckle(new Autodesk.AutoCAD.Geometry.Plane(extents.MinPoint, origin, corner));
          box = new Box(plane, xSize, ySize, zSize, ModelUnits) { area = area, volume = volume };
        }

        return box;
      }
      catch
      {
        return null;
      }
    }

    // Arcs
    public Arc ArcToSpeckle(AcadDB.Arc arc)
    {
      var _arc = new Arc(PlaneToSpeckle(arc.GetPlane()), arc.Radius, arc.StartAngle, arc.EndAngle, arc.TotalAngle, ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.midPoint = PointToSpeckle(arc.GetPointAtDist(arc.Length / 2));
      _arc.domain = new Interval(arc.StartParam, arc.EndParam);
      _arc.length = arc.Length;
      _arc.bbox = BoxToSpeckle(arc.GeometricExtents, true);
      return _arc;
    }
    public AcadDB.Arc ArcToNativeDB(Arc arc)
    {
      // because of different plane & start/end angle conventions, most reliable method to convert to autocad convention is to calculate from start, end, and midpoint
      var circularArc = ArcToNative(arc);

      // calculate adjusted start and end angles from circularArc reference
      double angle = circularArc.ReferenceVector.AngleOnPlane(PlaneToNative(arc.plane));
      double startAngle = circularArc.StartAngle + angle;
      double endAngle = circularArc.EndAngle + angle;

      var _arc = new AcadDB.Arc(
        PointToNative(arc.plane.origin), 
        VectorToNative(arc.plane.normal), 
        ScaleToNative((double)arc.radius, arc.units),
        startAngle,
        endAngle);

      return _arc;
    }

    // Circles
    public Circle CircleToSpeckle(AcadDB.Circle circle)
    {
      var _circle = new Circle(PlaneToSpeckle(circle.GetPlane()), circle.Radius, ModelUnits);
      _circle.length = circle.Circumference;
      _circle.bbox = BoxToSpeckle(circle.GeometricExtents, true);
      return _circle;

    }
    public AcadDB.Circle CircleToNativeDB(Circle circle)
    {
      var normal = VectorToNative(circle.plane.normal);
      var radius = ScaleToNative((double)circle.radius, circle.units);
      return new AcadDB.Circle(PointToNative(circle.plane.origin), normal, radius);
    }

    // Ellipses
    public Ellipse EllipseToSpeckle(AcadDB.Ellipse ellipse)
    {
      var plane = new Plane(ellipse.Center, ellipse.MajorAxis, ellipse.MinorAxis);
      var _ellipse = new Ellipse(PlaneToSpeckle(plane), ellipse.MajorRadius, ellipse.MinorRadius, ModelUnits);
      _ellipse.domain = new Interval(ellipse.StartParam, ellipse.EndParam);
      _ellipse.length = ellipse.GetDistanceAtParameter(ellipse.EndParam);
      _ellipse.bbox = BoxToSpeckle(ellipse.GeometricExtents, true);
      return _ellipse;
    }
    public AcadDB.Ellipse EllipseToNativeDB(Ellipse ellipse)
    {
      var normal = VectorToNative(ellipse.plane.normal);
      var xAxisVector = VectorToNative(ellipse.plane.xdir);
      var majorAxis = ScaleToNative((double)ellipse.firstRadius, ellipse.units) * xAxisVector.GetNormal();
      var radiusRatio = (double)ellipse.secondRadius / (double)ellipse.firstRadius;
      return new AcadDB.Ellipse(PointToNative(ellipse.plane.origin), normal, majorAxis, radiusRatio, 0, 2 * Math.PI);
    }

    // Rectangles
    public Polyline RectangleToSpeckle(Rectangle3d rectangle)
    {
      var vertices = new List<Point3d>() { rectangle.LowerLeft, rectangle.LowerRight, rectangle.UpperLeft, rectangle.UpperRight };
      return new Polyline(PointsToFlatArray(vertices), ModelUnits) { closed = true };
    }

    // Polycurves
    public Polyline PolylineToSpeckle(AcadDB.Polyline polyline) 
    {
      List<Point3d> vertices = new List<Point3d>();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
        vertices.Add(polyline.GetPoint3dAt(i));

      var _polyline = new Polyline(PointsToFlatArray(vertices), ModelUnits);
      _polyline.closed = polyline.Closed || polyline.StartPoint.Equals(polyline.EndPoint) ? true : false; // hatch boundary polylines are not closed, cannot rely on .Closed prop
      _polyline.length = polyline.Length;
      _polyline.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return _polyline;
    }

    public ICurve PolylineToSpeckle(AcadDB.BulgeVertexCollection bulges)
    {
      var polyline = new AcadDB.Polyline(bulges.Count);
      double totalBulge = 0;
      for (int i = 0; i < bulges.Count; i++)
      {
        BulgeVertex bulgeVertex = bulges[i];
        polyline.AddVertexAt(i, bulgeVertex.Vertex, bulgeVertex.Bulge, 1.0, 1.0);
        totalBulge += bulgeVertex.Bulge;
      }
      if (polyline.IsOnlyLines || totalBulge == 0)
        return PolylineToSpeckle(polyline);
      else
        return PolycurveToSpeckle(polyline);
    }

    public Polyline PolylineToSpeckle(AcadDB.Polyline3d polyline) // AC polyline3d can only have linear segments
    {
      List<Point3d> vertices = new List<Point3d>();

      // if this polyline is a new object, retrieve approximate vertices from spline nurbs data (should only be used for curve display value so far)
      if (polyline.IsNewObject)
      {
        foreach (Point3d vertex in polyline.Spline.NurbsData.GetControlPoints())
          vertices.Add(vertex);
      }
      // otherwise retrieve actual vertices from transaction
      else
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          foreach (ObjectId id in polyline)
          {
            var vertex = (PolylineVertex3d)tr.GetObject(id, OpenMode.ForRead);
            vertices.Add(vertex.Position);
          }
          tr.Commit();
        }
      }

      var _polyline = new Polyline(PointsToFlatArray(vertices), ModelUnits);
      _polyline.closed = polyline.Closed || polyline.StartPoint.Equals(polyline.EndPoint) ? true : false;
      _polyline.length = polyline.Length;
      _polyline.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return _polyline;
    }
    public Polyline3d PolylineToNativeDB(Polyline polyline) // AC polyline3d can only have linear segments
    {
      var vertices = new Point3dCollection();
      for (int i = 0; i < polyline.points.Count; i++)
        vertices.Add(PointToNative(polyline.points[i]));
      return new Polyline3d(Poly3dType.SimplePoly, vertices, polyline.closed);
    }
    public Polycurve PolycurveToSpeckle(Polyline2d polyline) // AC polyline2d can have linear, circlular, or elliptical segments
    {
      var polycurve = new Polycurve(units: ModelUnits) { closed = polyline.Closed };

      // extract segment curves
      var segments = new List<ICurve>();
      var exploded = new DBObjectCollection();
      polyline.Explode(exploded);
      Point3d previousPoint = new Point3d();
      for (int i = 0; i < exploded.Count; i++)
      {
        var segment = exploded[i] as AcadDB.Curve;

        if (i == 0 && exploded.Count > 1)
        {
          // get the connection point to the next segment - this is necessary since imported polycurves might have segments in different directions
          var connectionPoint = new Point3d();
          var nextSegment = exploded[i+1] as AcadDB.Curve;
          if (nextSegment.StartPoint.IsEqualTo(segment.StartPoint) || nextSegment.StartPoint.IsEqualTo(segment.EndPoint))
            connectionPoint = nextSegment.StartPoint;
          else
            connectionPoint = nextSegment.EndPoint;
          previousPoint = connectionPoint;
          segment = GetCorrectSegmentDirection(segment, connectionPoint, true, out Point3d otherPoint);
        }
        else
        {
          segment = GetCorrectSegmentDirection(segment, previousPoint, false, out previousPoint);
        }
        segments.Add(CurveToSpeckle(segment));
      }
      polycurve.segments = segments;

      polycurve.length = polyline.Length;
      polycurve.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return polycurve;
    }
    public Polycurve PolycurveToSpeckle(AcadDB.Polyline polyline) // AC polylines are polycurves with linear or arc segments
    {
      var polycurve = new Polycurve(units: ModelUnits) { closed = polyline.Closed };

      // extract segments
      var segments = new List<ICurve>();
      Point3d previousPoint = new Point3d();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
      {
        var segment = GetSegmentByType(polyline, i);
        if (segment == null)
          continue;
        if (i == 0 && polyline.NumberOfVertices > 1)
        {
          // get the connection point to the next segment
          var connectionPoint = new Point3d();
          var nextSegment = GetSegmentByType(polyline, i + 1);
          if (nextSegment.StartPoint.IsEqualTo(segment.StartPoint) || nextSegment.StartPoint.IsEqualTo(segment.EndPoint))
            connectionPoint = nextSegment.StartPoint;
          else
            connectionPoint = nextSegment.EndPoint;
          previousPoint = connectionPoint;
          segment = GetCorrectSegmentDirection(segment, connectionPoint, true, out Point3d otherPoint);
        }
        else
        {
          segment = GetCorrectSegmentDirection(segment, previousPoint, false, out previousPoint);
        }
        segments.Add(CurveToSpeckle(segment));
      }
      polycurve.segments = segments;

      polycurve.length = polyline.Length;
      polycurve.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return polycurve;
    }

    private Curve3d GetSegmentByType(AcadDB.Polyline polyline, int i)
    {
      SegmentType type = polyline.GetSegmentType(i);
      switch (type)
      {
        case SegmentType.Line:
          return polyline.GetLineSegmentAt(i);
        case SegmentType.Arc:
          return polyline.GetArcSegmentAt(i);
        default:
          return null;
      }
    }

    private AcadDB.Curve GetCorrectSegmentDirection (AcadDB.Curve segment, Point3d connectionPoint, bool isFirstSegment, out Point3d nextPoint) // note sometimes curve3d may not have endpoints
    {
      nextPoint = segment.EndPoint;

      if (connectionPoint == null)
        return segment;

      bool reverseDirection = false; 
      if (isFirstSegment)
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? true : false;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }  
      else
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? false : true;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }

      if (reverseDirection) segment.ReverseCurve();
      return segment;
    }
    private Curve3d GetCorrectSegmentDirection(Curve3d segment, Point3d connectionPoint, bool isFirstSegment, out Point3d nextPoint) // note sometimes curve3d may not have endpoints
    {
      nextPoint = segment.EndPoint;

      if (connectionPoint == null)
        return segment;

      bool reverseDirection = false;
      if (isFirstSegment)
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? true : false;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }
      else
      {
        reverseDirection = (segment.StartPoint.IsEqualTo(connectionPoint)) ? false : true;
        if (reverseDirection) nextPoint = segment.StartPoint;
      }

      return (reverseDirection) ? segment.GetReverseParameterCurve() : segment;
    }

    // polylines can only support curve segments of type circular arc
    // currently, this will collapse 3d polycurves into 2d since there is no polycurve class that can contain 3d polylines with nonlinear segments
    public AcadDB.Polyline PolycurveToNativeDB(Polycurve polycurve) 
    {
      AcadDB.Polyline polyline = new AcadDB.Polyline() { Closed = polycurve.closed };
      var plane = new Autodesk.AutoCAD.Geometry.Plane(Point3d.Origin, Vector3d.ZAxis.TransformBy(Doc.Editor.CurrentUserCoordinateSystem)); // TODO: check this 
      
      // add all vertices
      for (int i = 0; i < polycurve.segments.Count; i++)
      {
        var segment = polycurve.segments[i];
        switch (segment)
        {
          case Line o:
            polyline.AddVertexAt(i, PointToNative(o.start).Convert2d(plane), 0, 0, 0);
            if (!polycurve.closed && i == polycurve.segments.Count - 1)
              polyline.AddVertexAt(i + 1, PointToNative(o.end).Convert2d(plane), 0, 0, 0);
            break;
          case Arc o:
            var bulge = Math.Tan((double)(o.endAngle - o.startAngle) / 4) * BulgeDirection(o.startPoint, o.midPoint, o.endPoint); // bulge
            polyline.AddVertexAt(i, PointToNative(o.startPoint).Convert2d(plane), bulge, 0, 0);
            if (!polycurve.closed && i == polycurve.segments.Count - 1)
              polyline.AddVertexAt(i + 1, PointToNative(o.endPoint).Convert2d(plane), 0, 0, 0);
            break;
          default:
            return null;
        }
      }

      return polyline;
    }
    // handles polycurves with spline segments: bakes segments individually and then joins
    // TODO: can use this for 3d polycurves with arc segments (needs an IsPlanar property)
    public AcadDB.Spline PolycurveSplineToNativeDB(Polycurve polycurve)
    {
      AcadDB.Curve firstSegment = CurveToNativeDB(polycurve.segments[0]);
      List<AcadDB.Curve> otherSegments = new List<AcadDB.Curve>();
      for (int i = 1; i < polycurve.segments.Count; i++)
      {
        var converted = CurveToNativeDB(polycurve.segments[i]);
        if (converted == null)
          return null;
        otherSegments.Add(converted);
      }
      firstSegment.JoinEntities(otherSegments.ToArray());
      return firstSegment.Spline;
    }

    // calculates bulge direction: (-) clockwise, (+) counterclockwise
    int BulgeDirection(Point start, Point mid, Point end)
    {
      // get vectors from points
      double[] v1 = new double[] { end.x - start.x, end.y - start.y, end.z - start.z }; // vector from start to end point
      double[] v2 = new double[] { mid.x - start.x, mid.y - start.y, mid.z - start.z }; // vector from start to mid point

      // calculate cross product z direction
      double z = v1[0] * v2[1] - v2[0] * v1[1];

      if (z > 0)
        return -1;
      else
        return 1;
    }

    // Splines
    public Curve SplineToSpeckle(Spline spline)
    {
      var curve = new Curve();

      // get nurbs and geo data 
      var data = spline.NurbsData;
      var _spline = spline.GetGeCurve() as NurbCurve3d;

      // hack: check for incorrectly closed periodic curves (this seems like acad bug, has resulted from receiving rhino curves)
      bool periodicClosed = false;
      if (_spline.Knots.Count < _spline.NumberOfControlPoints + _spline.Degree + 1 && spline.IsPeriodic)
        periodicClosed = true;

      // handle the display polyline
      try
      {
        var poly = spline.ToPolyline(false, true);
        Polyline displayValue = ConvertToSpeckle(poly) as Polyline;
        curve.displayValue = displayValue;
      }
      catch { }

      // get points
      // NOTE: for closed periodic splines, autocad does not track last #degree points. Add the first #degree control points to the list if so.
      var points = data.GetControlPoints().OfType<Point3d>().ToList();
      if (periodicClosed)
        points.AddRange(points.GetRange(0, spline.Degree));

      // get knots
      // NOTE: for closed periodic splines, autocad has #control points + 1 knots. Add #degree extra knots to beginning and end with #degree - 1 multiplicity for first and last
      var knots = data.GetKnots().OfType<double>().ToList();
      if (periodicClosed)
      {
        double interval = knots[1] - knots[0]; //knot interval

        for (int i = 0; i < data.Degree; i++)
        {
          if (i < 2)
          {
            knots.Insert(knots.Count, knots[knots.Count - 1] + interval);
            knots.Insert(0, knots[0] - interval);
          }
          else
          {
            knots.Insert(knots.Count, knots[knots.Count - 1]);
            knots.Insert(0, knots[0]);
          }
        }
      }

      // get weights
      // NOTE: autocad assigns unweighted points a value of -1, and will return an empty list in the spline's nurbsdata if no points are weighted
      // NOTE: for closed periodic splines, autocad does not track last #degree points. Add the first #degree weights to the list if so.
      var weights = new List<double>();
      for (int i = 0; i < spline.NumControlPoints; i++)
      {
        double weight = spline.WeightAt(i);
        if (weight <= 0)
          weights.Add(1);
        else
          weights.Add(weight);
      }
      if (periodicClosed)
        weights.AddRange(weights.GetRange(0, spline.Degree));

      // set nurbs curve info
      curve.points = PointsToFlatArray(points).ToList();
      curve.knots = knots;
      curve.weights = weights;
      curve.degree = spline.Degree;
      curve.periodic = spline.IsPeriodic;
      curve.rational = spline.IsRational;
      curve.closed = (periodicClosed) ? true : spline.Closed;
      curve.length = _spline.GetLength(_spline.StartParameter, _spline.EndParameter, tolerance);
      curve.domain = IntervalToSpeckle(_spline.GetInterval());
      curve.bbox = BoxToSpeckle(spline.GeometricExtents, true);
      curve.units = ModelUnits;

      return curve;
    }

    public AcadDB.Curve NurbsToNativeDB(Curve curve)
    {
      var _curve = AcadDB.Curve.CreateFromGeCurve(NurbcurveToNative(curve));
      return _curve;
    }

    // All curves
    public AcadDB.Curve CurveToNativeDB(ICurve icurve)
    {
      switch (icurve)
      {
        case Line line:
          return LineToNativeDB(line);

        case Polyline polyline:
          return PolylineToNativeDB(polyline);

        case Arc arc:
          return ArcToNativeDB(arc);

        case Circle circle:
          return CircleToNativeDB(circle);

        case Ellipse ellipse:
          return EllipseToNativeDB(ellipse);

        case Polycurve polycurve:
          return PolycurveToNativeDB(polycurve);

        case Curve curve:
          return NurbsToNativeDB(curve);

        default:
          return null;
      }
    }

    public ICurve CurveToSpeckle(AcadDB.Curve curve, string units = null)
    {
      var u = units ?? ModelUnits;

      switch (curve)
      {
        case AcadDB.Line line:
          return LineToSpeckle(line, u);

        case AcadDB.Polyline polyline:
          if (polyline.IsOnlyLines)
            return PolylineToSpeckle(polyline);
          else 
            return PolycurveToSpeckle(polyline);

        case AcadDB.Polyline2d polyline2d:
          return PolycurveToSpeckle(polyline2d);

        case AcadDB.Polyline3d polyline3d:
          return PolylineToSpeckle(polyline3d);

        case AcadDB.Arc arc:
          return ArcToSpeckle(arc);

        case AcadDB.Circle circle:
          return CircleToSpeckle(circle);

        case AcadDB.Ellipse ellipse:
          return EllipseToSpeckle(ellipse);

        case AcadDB.Spline spline:
          return SplineToSpeckle(spline);

        default:
          return null;
      }
    }

    // Regions
    public Mesh RegionToSpeckle(Region region, string units = null)
    {
      var u = units ?? ModelUnits;

      return GetMeshFromSolidOrSurface(region: region);
    }

    // Hatches
    public Hatch HatchToSpeckle(AcadDB.Hatch hatch)
    {
      var _hatch = new Hatch();
      _hatch.pattern = hatch.PatternName;
      _hatch.scale = hatch.PatternScale;
      _hatch.rotation = hatch.PatternAngle;

      // handle curves
      var curves = new List<ICurve>();
      for (int i = 0; i < hatch.NumberOfLoops; i++)
      {
        var loop = hatch.GetLoopAt(i);
        if (loop.IsPolyline)
        {
          curves.Add(PolylineToSpeckle(loop.Polyline));
        }
        else
        {
          var loopcurves = hatch.GetLoopAt(i).Curves;
          if (loopcurves != null)
            foreach (AcadDB.Curve loopcurve in loopcurves)
              curves.Add(CurveToSpeckle(loopcurve));
        }
      }
      _hatch.curves = curves;

      return _hatch;
    }
    public AcadDB.Hatch HatchToNativeDB(Hatch hatch)
    {
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();

      // convert curves
      var curveIds = new ObjectIdCollection();
      var curves = new List<DBObject>();
      foreach (var curve in hatch.curves)
      {
        var converted = CurveToNativeDB(curve);
        if (converted == null || !converted.Closed)
          return null;
        if (converted.IsNewObject)
        {
          var curveId = modelSpaceRecord.Append(converted);
          if (curveId.IsValid)
          {
            curveIds.Add(curveId);
            curves.Add(converted);
          }
        }
      }

      // add hatch to modelspace
      var _hatch = new AcadDB.Hatch();
      modelSpaceRecord.Append(_hatch);

      _hatch.SetDatabaseDefaults();
      // try get hatch pattern
      switch (HatchPatterns.ValidPatternName(hatch.pattern))
      {
        case PatPatternCategory.kCustomdef:
          _hatch.SetHatchPattern(HatchPatternType.CustomDefined, hatch.pattern);
          break;
        case PatPatternCategory.kPredef:
          _hatch.SetHatchPattern(HatchPatternType.PreDefined, hatch.pattern);
          break;
        case PatPatternCategory.kUserdef:
          _hatch.SetHatchPattern(HatchPatternType.UserDefined, hatch.pattern);
          break;
        default:
          _hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
          break;
      }
      _hatch.PatternAngle = hatch.rotation;
      _hatch.PatternScale = hatch.scale;
      _hatch.AppendLoop(HatchLoopTypes.Default, curveIds);
      _hatch.EvaluateHatch(true);

      // delete created hatch curves
      foreach (DBObject curve in curves)
        curve.Erase();

      return _hatch;
    }

    // Surfaces
    public Mesh SurfaceToSpeckle(AcadDB.Surface surface, string units = null)
    {
      var u = units ?? ModelUnits;

      switch (surface)
      {
        case AcadDB.PlaneSurface _:
        case AcadDB.NurbSurface _:
        default: // return mesh for now
          var displayMesh = GetMeshFromSolidOrSurface(surface: surface);
          return displayMesh;
      }
    }

    public Surface SurfaceToSpeckle(AcadDB.NurbSurface surface, string units = null)
    {
      var u = units ?? ModelUnits;

      List<double> Uknots = new List<double>();
      List<double> Vknots = new List<double>();
      foreach (var knot in surface.UKnots)
        Uknots.Add((double)knot);
      foreach (var knot in surface.VKnots)
        Vknots.Add((double)knot);

      var _surface = new Surface
      {
        degreeU = surface.DegreeInU,
        degreeV = surface.DegreeInV,
        rational = surface.IsRational,
        closedU = surface.IsClosedInU,
        closedV = surface.IsClosedInV,
        knotsU = Uknots,
        knotsV = Vknots,
        countU = surface.NumberOfControlPointsInU,
        countV = surface.NumberOfControlPointsInV
      };
      _surface.SetControlPoints(ControlPointsToSpeckle(surface));
      _surface.bbox = BoxToSpeckle(surface.GeometricExtents, true);
      _surface.units = ModelUnits;

      return _surface;
    }
    public AcadDB.Surface SurfaceToNativeDB(Surface surface)
    {
      // get control points
      Point3dCollection controlPoints = new Point3dCollection();
      DoubleCollection weights = new DoubleCollection();
      var points = surface.GetControlPoints();
      for (var i = 0; i < points.Count; i++)
      {
        for (var j = 0; j < points[i].Count; j++)
        {
          var pt = points[i][j];
          controlPoints.Add(PointToNative(pt));
          weights.Add(pt.weight);
        }
      }

      // get knots
      var knotsU = new KnotCollection();
      var knotsV = new KnotCollection();
      foreach (var knotU in surface.knotsU)
        knotsU.Add(knotU);
      foreach (var knotV in surface.knotsV)
        knotsV.Add(knotV);

      var _surface = new AcadDB.NurbSurface(
        surface.degreeU,
        surface.degreeV,
        surface.rational,
        surface.countU,
        surface.countV,
        controlPoints,
        weights,
        knotsU,
        knotsV);

      return _surface;
    }
    

    // Meshes
    /* need edge & face info on polygon meshes
    public Mesh MeshToSpeckle(AcadDB.PolygonMesh mesh)
    {
      var _vertices = new List<Point3d>();
      var colors = new List<int>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in mesh)
        {
          var vertex = (PolygonMeshVertex)tr.GetObject(id, OpenMode.ForRead);
          _vertices.Add(vertex.Position);
          colors.Add(vertex.Color.ColorValue.ToArgb());
        }
        tr.Commit();
      }
      var vertices = PointsToFlatArray(_vertices);

      var speckleMesh = new Mesh(vertices, faces, colors.ToArray(), null, ModelUnits);
      speckleMesh.bbox = BoxToSpeckle(mesh.GeometricExtents, true);

      return speckleMesh;
    }
    */
      // Polyface mesh vertex indexing starts at 1. Subtract 1 from face vertex index when sending to Speckle
      public Mesh MeshToSpeckle(AcadDB.PolyFaceMesh mesh, string units = null)
    {
      var u = units ?? ModelUnits;

      var _vertices = new List<Point3d>();
      var _faces = new List<int[]>();
      var colors = new List<int>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in mesh)
        {
          DBObject obj = tr.GetObject(id, OpenMode.ForRead);
          switch (obj)
          {
            case PolyFaceMeshVertex o:
              _vertices.Add(o.Position);
              colors.Add(o.Color.ColorValue.ToArgb());
              break;
            case FaceRecord o:
              var indices = new List<int>();
              for (short i = 0; i < 4; i++)
              {
                short index = o.GetVertexAt(i);
                if (index != 0)
                  indices.Add(index);
              }
              if (indices.Count == 4) // vertex index starts at 1 sigh
                _faces.Add(new int[] { 1, indices[0] - 1, indices[1] - 1, indices[2] - 1, indices[3] - 1 });
              else
                _faces.Add(new int[] { 0, indices[0] - 1, indices[1] - 1, indices[2] - 1 });
              break;
          }
        }
        tr.Commit();
      }
      var vertices = PointsToFlatArray(_vertices);
      var faces = _faces.SelectMany(o => o).ToArray();

      var speckleMesh = new Mesh(vertices, faces, colors.ToArray(), null, u);
      speckleMesh.bbox = BoxToSpeckle(mesh.GeometricExtents, true);

      return speckleMesh;
    }
    public Mesh MeshToSpeckle(AcadDB.SubDMesh mesh)
    {
      // vertices
      var _vertices = new List<Point3d>();
      foreach (Point3d point in mesh.Vertices)
        _vertices.Add(point);
      var vertices = PointsToFlatArray(_vertices);

      // faces
      var _faces = new List<int[]>();
      int[] faceArr = mesh.FaceArray.ToArray(); // contains vertex indices
      int edgeCount = 0;
      for (int i = 0; i < faceArr.Length; i = i + edgeCount + 1)
      {
        List<int> faceVertices = new List<int>();
        edgeCount = faceArr[i];
        for (int j = i + 1; j <= i + edgeCount; j++)
          faceVertices.Add(faceArr[j]);
        if (edgeCount == 4) // quad face
          _faces.Add(new int[] { 1, faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3] });
        else // triangle face
          _faces.Add(new int[] { 0, faceVertices[0], faceVertices[1], faceVertices[2] });
      }
      var faces = _faces.SelectMany(o => o).ToArray();

      // colors
      var colors = mesh.VertexColorArray.Select(o => Color.FromArgb(Convert.ToInt32(o.Red), Convert.ToInt32(o.Green), Convert.ToInt32(o.Blue)).ToArgb()).ToArray();
      
      var speckleMesh = new Mesh(vertices, faces, colors, null, ModelUnits);
      speckleMesh.bbox = BoxToSpeckle(mesh.GeometricExtents, true);

      return speckleMesh;
    }

    // Polyface mesh vertex indexing starts at 1. Add 1 to face vertex index when converting to native
    public AcadDB.PolyFaceMesh MeshToNativeDB(Mesh mesh)
    {
      // get vertex points
      var vertices = new Point3dCollection();
      Point3d[] points = PointListToNative(mesh.vertices, mesh.units);
      foreach (var point in points)
        vertices.Add(point);

      PolyFaceMesh _mesh = null;

      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        _mesh = new PolyFaceMesh();
        _mesh.SetDatabaseDefaults();

        // append mesh to blocktable record - necessary before adding vertices and faces
        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite);
        btr.AppendEntity(_mesh);
        tr.AddNewlyCreatedDBObject(_mesh, true);

        // add polyfacemesh vertices
        for (int i = 0; i < vertices.Count; i++)
        {
          var vertex = new PolyFaceMeshVertex(points[i]);
          if (mesh.colors.Count > 0)
          {
            try
            {
              Color color = Color.FromArgb(mesh.colors[i]);
              vertex.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(color.R, color.G, color.B);
            }
            catch { }
          }
          if (vertex.IsNewObject)
          {
            _mesh.AppendVertex(vertex);
            tr.AddNewlyCreatedDBObject(vertex, true);
          }
        }

        // add polyfacemesh faces. vertex index starts at 1 sigh
        int j = 0;
        while (j < mesh.faces.Count)
        {
          FaceRecord face = null;
          if (mesh.faces[j] == 0) // triangle
          {
            face = new FaceRecord((short)(mesh.faces[j + 1] + 1), (short)(mesh.faces[j + 2] + 1), (short)(mesh.faces[j + 3] + 1), 0);
            j += 4;
          }
          else // quad
          {
            face = new FaceRecord((short)(mesh.faces[j + 1] + 1), (short)(mesh.faces[j + 2] + 1), (short)(mesh.faces[j + 3] + 1), (short)(mesh.faces[j + 4] + 1));
            j += 5;
          }
          if (face != null)
          {
            if (face.IsNewObject)
            {
              _mesh.AppendFaceRecord(face);
              tr.AddNewlyCreatedDBObject(face, true);
            }
          }
        }

        tr.Commit();
      }
     
      return _mesh;
    }

    // breps
    public Mesh SolidToSpeckle(Solid3d solid, string units = null)
    {
      var u = units ?? ModelUnits;

      // create display mesh
      var displayMesh = GetMeshFromSolidOrSurface(solid);

      return displayMesh;

      /* Not in use currently: needs development on trims
      // make brep
      var brep = new AcadBRep.Brep(solid);
      var t = brep.Faces.First().GetSurfaceAsTrimmedNurbs()[0].GetContours();

      // output lists
      var speckleBrep = new Brep(displayValue: displayMesh, provenance: Applications.Autocad2021, units: u);
      var speckleFaces = new List<BrepFace>();
      var speckleLoops = new List<BrepLoop>();
      var speckleSurfaces = new List<Surface>();
      var speckleTrims = new List<BrepTrim>();
      var speckleEdges = new List<BrepEdge>();
      var SpeckleCurve2ds = new List<ICurve>();

      // process vertices
      var vertexList = brep.Vertices.ToList();
      var speckleVertices = vertexList.Select(o => PointToSpeckle(o.Point, u)).ToList();

      // process faces, surfaces, loops, curve3ds
      var faceList = new List<AcadBRep.Face>();
      var loopList = new List<AcadBRep.BoundaryLoop>();
      var curve3dList = new List<Curve3d>();
      for (int i = 0; i < brep.Faces.Count(); i++)
      {
        var face = brep.Faces.ElementAt(i);
        faceList.Add(face);

        // surfaces
        speckleSurfaces.Add(SurfaceToSpeckle(face.GetSurfaceAsNurb(), u));

        // curve3ds
        var boundaries = face.GetSurfaceAsTrimmedNurbs().First().GetContours();
        foreach (var boundary in boundaries)
          foreach (var contour in boundary.Contour.GetCurve3ds().Select(o => (Curve3d)o))
            if (curve3dList.Where(o => o.IsEqualTo(contour)).Count() == 0)
              curve3dList.Add(contour);

        // loops
        var loops = new List<int>();
        int count = loopList.Count;
        int outerLoop = count;
        foreach (var loop in face.Loops)
        {
          loopList.Add(loop); loops.Add(count);
          if (loop.LoopType == AcadBRep.LoopType.LoopExterior)
            outerLoop = count;
          var speckleLoop = new BrepLoop(speckleBrep, i, null, GetLoopType(loop.LoopType));
          speckleLoops.Add(speckleLoop);
          count++;
        }
        var speckleFace = new BrepFace(speckleBrep, i, loops, outerLoop, !face.IsOrientToSurface);
        speckleFaces.Add(speckleFace);
      }
      var speckleCurve3ds = curve3dList.Select(o => CurveToSpeckle(o)).ToList();

      // process edges
      var edgeDictionary = new Dictionary<AcadBRep.Edge, int>();
      for (int i = 0; i < brep.Edges.Count(); i++)
      {
        var edge = brep.Edges.ElementAt(i);
        edgeDictionary.Add(edge, i);

        var startIndex = GetIndexOfVertex(vertexList, edge.Vertex1);
        var endIndex = GetIndexOfVertex(vertexList, edge.Vertex2);
        var crvIndex = GetIndexOfCurve(curve3dList,edge.Curve);

        var speckleEdge = new BrepEdge(speckleBrep, crvIndex, null, startIndex, endIndex, !edge.IsOrientToCurve, IntervalToSpeckle(edge.Curve.GetInterval()));
        speckleEdges.Add(speckleEdge);
      }

      // set props
      speckleBrep.Curve3D = speckleCurve3ds;
      speckleBrep.Edges = speckleEdges;
      speckleBrep.Faces = speckleFaces;
      speckleBrep.Surfaces = speckleSurfaces;
      speckleBrep.Vertices = speckleVertices;
      speckleBrep.Loops = speckleLoops;

      speckleBrep.IsClosed = true;
      speckleBrep.Orientation = Geometry.BrepOrientation.Unknown;
      speckleBrep.volume = brep.GetVolume();
      speckleBrep.bbox = BoxToSpeckle(brep.BoundBlock);
      speckleBrep.area = brep.GetSurfaceArea();
      return speckleBrep;
      */
    }

    // Based on Kean Walmsley's blog post on mesh conversion using Brep API
    private Mesh GetMeshFromSolidOrSurface(Solid3d solid = null, AcadDB.Surface surface = null, Region region = null)
    {
      Mesh mesh = null;

      AcadBRep.Brep brep = null;
      if (solid != null)
        brep = new AcadBRep.Brep(solid);
      else if (surface != null)
        brep = new AcadBRep.Brep(surface);
      else if (region != null)
        brep = new AcadBRep.Brep(region);

      if (brep!= null)
      {
        using (var control = new AcadBRep.Mesh2dControl())
        {
          // These settings may need adjusting
          control.MaxSubdivisions = 10000;

          // output mesh vars
          var _vertices = new List<Point3d>();
          var _faces = new List<int[]>();

          // create mesh filterS
          using (var filter = new AcadBRep.Mesh2dFilter())
          {
            filter.Insert(brep, control);
            using (var m = new AcadBRep.Mesh2d(filter))
            {
              foreach (var e in m.Element2ds)
              {
                // get vertices
                var faceIndices = new List<int>();
                foreach (var n in e.Nodes)
                {
                  if (!_vertices.Contains(n.Point))
                  {
                    faceIndices.Add(_vertices.Count);
                    _vertices.Add(n.Point);
                  }
                  else
                  {
                    faceIndices.Add(_vertices.IndexOf(n.Point));
                  }
                  n.Dispose();
                }

                // get faces
                if (e.Nodes.Count() == 3)
                  _faces.Add(new int[] { 0, faceIndices[0], faceIndices[1], faceIndices[2] });
                else if (e.Nodes.Count() == 4)
                  _faces.Add(new int[] { 1, faceIndices[0], faceIndices[1], faceIndices[2], faceIndices[3] });
                e.Dispose();
              }
            }
          }
          brep.Dispose();

          // create speckle mesh
          var vertices = PointsToFlatArray(_vertices);
          var faces = _faces.SelectMany(o => o).ToArray();
          mesh = new Mesh(vertices, faces);
          mesh.units = ModelUnits;
          if (solid != null)
            mesh.bbox = BoxToSpeckle(solid.GeometricExtents);
          else if (surface != null)
            mesh.bbox = BoxToSpeckle(surface.GeometricExtents);
          else if (region != null)
            mesh.bbox = BoxToSpeckle(region.GeometricExtents);
        }
      }

      return mesh;
    }


    private int GetIndexOfCurve(List<Curve3d> list, Curve3d curve) // necessary since contains comparer doesn't work
    {
      int index = -1;
      for (int i = 0; i < list.Count; i++)
      {
        if (list[i].IsEqualTo(curve))
        {
          index = i;
          break;
        }
      }
      return index;
    }

    private int GetIndexOfVertex(List<AcadBRep.Vertex> list, AcadBRep.Vertex vertex)
    {
      int index = -1;
      for (int i = 0; i < list.Count; i++)
      {
        if (list[i].Point.IsEqualTo(vertex.Point))
        {
          index = i;
          break;
        }
      }
      return index;
    }

    private BrepLoopType GetLoopType(AcadBRep.LoopType loopType)
    {
      switch (loopType)
      {
        case AcadBRep.LoopType.LoopExterior:
          return BrepLoopType.Outer;
        case AcadBRep.LoopType.LoopInterior:
          return BrepLoopType.Inner;
        default:
          return BrepLoopType.Unknown;
      }
    }

    // blocks

    public BlockInstance BlockReferenceToSpeckle(AcadDB.BlockReference reference)
    {
      // skip if dynamic block
      if (reference.IsDynamicBlock)
        return null;

      // get record
      BlockDefinition definition = null;
      var attributes = new Dictionary<string, string>();
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
        definition = BlockRecordToSpeckle(btr);
        foreach (ObjectId id in reference.AttributeCollection)
        {
          AttributeReference attRef = (AttributeReference)tr.GetObject(id, OpenMode.ForRead);
          attributes.Add(attRef.Tag, attRef.TextString);
        }

        tr.Commit();
      }
      
      if (definition == null)
        return null;

      var instance = new BlockInstance()
      {
        insertionPoint = PointToSpeckle(reference.Position),
        transform = reference.BlockTransform.ToArray(),
        blockDefinition = definition,
        units = ModelUnits
      };
      
      // add attributes
      foreach (var attribute in attributes)
        instance[attribute.Key] = attribute.Value;

      return instance;
    }
    public string BlockInstanceToNativeDB(BlockInstance instance, out BlockReference reference, bool AppendToModelSpace = true)
    {
      string result = null;
      reference = null;

      // block definition
      ObjectId definitionId = BlockDefinitionToNativeDB(instance.blockDefinition);
      if (definitionId == ObjectId.Null)
        return result;

      // insertion pt
      Point3d insertionPoint = PointToNative(instance.insertionPoint);

      // transform
      double[] transform = instance.transform;
      for (int i = 3; i < 12; i += 4)
        transform[i] = ScaleToNative(transform[i], instance.units);
      Matrix3d convertedTransform = new Matrix3d(transform);

      // add block reference
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();
      BlockReference br = new BlockReference(insertionPoint, definitionId);
      br.BlockTransform = convertedTransform;
      ObjectId id = ObjectId.Null;
      if (AppendToModelSpace)
        id = modelSpaceRecord.Append(br);

      // return
      result = "success";
      if ((id.IsValid && !id.IsNull) || !AppendToModelSpace)
        reference = br;

      return result;
    }

    public BlockDefinition BlockRecordToSpeckle (BlockTableRecord record)
    {
      // skip if this is from an external reference
      if (record.IsFromExternalReference)
        return null;

      // get geometry
      var geometry = new List<Base>();
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in record)
        {
          DBObject obj = tr.GetObject(id, OpenMode.ForRead);
          Entity objEntity = obj as Entity;
          if (CanConvertToSpeckle(obj))
          {
            Base converted = ConvertToSpeckle(obj);
            if (converted != null)
            {
              converted["Layer"] = objEntity.Layer;
              geometry.Add(converted);
            }
          }
        }

        tr.Commit();
      }

      var definition = new BlockDefinition()
      {
        name = record.Name,
        basePoint = PointToSpeckle(record.Origin),
        geometry = geometry,
        units = ModelUnits
      };

      return definition;
    }

    public ObjectId BlockDefinitionToNativeDB(BlockDefinition definition)
    {
      // get modified definition name with commit info
      var blockName = $"{Doc.UserData["commit"]} - {RemoveInvalidChars(definition.name)}";

      ObjectId blockId = ObjectId.Null;

      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        // see if block record already exists and return if so
        BlockTable blckTbl = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (blckTbl.Has(blockName))
        {
          tr.Commit();
          return blckTbl[blockName];
        }

        // create btr
        using (BlockTableRecord btr = new BlockTableRecord())
        {
          btr.Name = blockName;

          // base point
          btr.Origin = PointToNative(definition.basePoint);

          // add geometry
          blckTbl.UpgradeOpen();
          var bakedGeometry = new ObjectIdCollection(); // this is to contain block def geometry that is already added to doc space during conversion
          foreach (var geo in definition.geometry)
          {
            if (CanConvertToNative(geo))
            {
              Entity converted = null;
              switch (geo)
              {
                case BlockInstance o:
                  BlockInstanceToNativeDB(o, out BlockReference reference, false);
                  converted = reference;
                  break;
                default:
                  converted = ConvertToNative(geo) as Entity;
                  break;
              }

              if (converted == null)
                continue;
              else if (!converted.IsNewObject && !(converted is BlockReference))
                bakedGeometry.Add(converted.Id);
              else
                btr.AppendEntity(converted);
            }
          }
          blockId = blckTbl.Add(btr);
          btr.AssumeOwnershipOf(bakedGeometry); // add in baked geo
          tr.AddNewlyCreatedDBObject(btr, true);
          blckTbl.Dispose();
        }

        tr.Commit();
      }

      return blockId;
    }

    // Text
    public Text TextToSpeckle(AcadDB.DBText text)
    {
      var _text = new Text();

      // not realistically feasible to extract outline curves for displayvalue currently
      _text.height = text.Height;
      var center = GetTextCenter(text);
      _text.plane = PlaneToSpeckle( new Plane(center, text.Normal));
      _text.rotation = text.Rotation;
      _text.value = text.TextString;
      _text.units = ModelUnits;

      // autocad specific props
      _text["horizontalAlignment"] = text.HorizontalMode.ToString();
      _text["verticalAlignment"] = text.VerticalMode.ToString();
      _text["position"] = PointToSpeckle(text.Position);
      _text["widthFactor"] = text.WidthFactor;
      _text["isMText"] = false;

      return _text;
    }
    public Text TextToSpeckle(AcadDB.MText text)
    {
      var _text = new Text();

      // not realistically feasible to extract outline curves for displayvalue currently
      _text.height = text.Height;
      var center = (text.Bounds != null) ? GetTextCenter(text.Bounds.Value) : text.Location;
      _text.plane = PlaneToSpeckle( new Plane(center, text.Normal));
      _text.rotation = text.Rotation;    
      _text.value = text.Contents;
      _text.richText = text.ContentsRTF;
      _text.units = ModelUnits;

      // autocad specific props
      _text["position"] = PointToSpeckle(text.Location);
      _text["isMText"] = true;

      return _text;
    }
    public MText MTextToNative(Text text)
    {
      var _text = new MText();

      if (string.IsNullOrEmpty(text.richText))
        _text.Contents = text.value;
      else
        _text.ContentsRTF = text.richText;
      _text.TextHeight = ScaleToNative(text.height, text.units);
      _text.Location = (text["position"] != null) ? PointToNative(text["position"] as Point) : PointToNative(text.plane.origin);
      _text.Rotation = text.rotation;
      _text.Normal = VectorToNative(text.plane.normal);

      return _text;
    }
    public DBText DBTextToNative(Text text)
    {
      var _text = new DBText();
      _text.TextString = text.value;
      _text.Height = ScaleToNative(text.height, text.units);
      _text.Position = (text["position"] != null) ? PointToNative(text["position"] as Point) : PointToNative(text.plane.origin);
      _text.Rotation = text.rotation;
      _text.Normal = VectorToNative(text.plane.normal);
      double widthFactor = text["widthFactor"] as double? ?? 1;
      _text.WidthFactor = widthFactor;

      return _text;
    }
    private Point3d GetTextCenter(Extents3d extents)
    {
      var x = (extents.MaxPoint.X + extents.MinPoint.X) / 2.0;
      var y = (extents.MaxPoint.Y + extents.MinPoint.Y) / 2.0;
      var z = (extents.MaxPoint.Z + extents.MinPoint.Z) / 2.0;

      return new Point3d(x, y, z);
    }
    private Point3d GetTextCenter(DBText text)
    {
      var position = text.Position;
      double x = position.X; double y = position.Y; double z = position.Z;

      if (text.Bounds != null)
      {
        var extents = text.Bounds.Value;
        x = (extents.MaxPoint.X + extents.MinPoint.X) / 2.0;
        y = (extents.MaxPoint.Y + extents.MinPoint.Y) / 2.0;
        z = (extents.MaxPoint.Z + extents.MinPoint.Z) / 2.0;

        return new Point3d(x, y, z);
      }

      var alignment = text.AlignmentPoint;
      var height = text.Height;
      switch (text.Justify)
      {
        case AttachmentPoint.BottomMid:
        case AttachmentPoint.BottomCenter:
          x = alignment.X;  y = alignment.Y + (height / 2);
          break;
        case AttachmentPoint.TopCenter:
        case AttachmentPoint.TopMid:
          x = alignment.X;  y = alignment.Y - (height / 2);
          break;
        case AttachmentPoint.MiddleRight:
          x = alignment.X - ((alignment.X - position.X) / 2); y = alignment.Y;
          break;
        case AttachmentPoint.BottomRight:
          x = alignment.X - ((alignment.X - position.X) / 2); y = alignment.Y + (height / 2);
          break;
        case AttachmentPoint.TopRight:
          x = alignment.X - ((alignment.X - position.X) / 2); y = alignment.Y - (height / 2);
          break;
        case AttachmentPoint.MiddleCenter:
        case AttachmentPoint.MiddleMid:
          x = alignment.X; y = alignment.Y;
          break;
        default:
          break;
      }
      return new Point3d(x, y, z);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
using System.Drawing;

using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using ControlPoint = Objects.Geometry.ControlPoint;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Surface = Objects.Geometry.Surface;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Speckle.Core.Models;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // Points
    public Point PointToSpeckle(DBPoint point)
    {
      return PointToSpeckle(point.Position);
    }
    public DBPoint PointToNativeDB(Point point)
    {
      return new DBPoint(PointToNative(point));
    }

    // Lines
    public Line LineToSpeckle(AcadDB.Line line)
    {
      var _line = new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), ModelUnits);
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
    // TODO: fix major/minor vs x axis/yaxis distinction in conversions after speckle firstRadius & secondRadius def is set
    public Ellipse EllipseToSpeckle(AcadDB.Ellipse ellipse)
    {
      var _ellipse = new Ellipse(PlaneToSpeckle(ellipse.GetPlane()), ellipse.MajorRadius, ellipse.MinorRadius, ModelUnits);
      _ellipse.domain = new Interval(ellipse.StartParam, ellipse.EndParam);
      _ellipse.length = ellipse.GetDistanceAtParameter(ellipse.EndParam);
      _ellipse.bbox = BoxToSpeckle(ellipse.GeometricExtents, true);
      return _ellipse;
    }
    public AcadDB.Ellipse EllipseToNativeDB(Ellipse ellipse)
    {
      var normal = VectorToNative(ellipse.plane.normal);
      var majorAxis = ScaleToNative((double)ellipse.firstRadius, ellipse.units) * VectorToNative(ellipse.plane.xdir);
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
    public Polyline PolylineToSpeckle(AcadDB.Polyline polyline) // AC polylines can have arc segments, this treats all segments as lines
    {
      List<Point3d> vertices = new List<Point3d>();
      for (int i = 0; i < polyline.NumberOfVertices; i++)
        vertices.Add(polyline.GetPoint3dAt(i));
      if (polyline.Closed)
        vertices.Add(polyline.GetPoint3dAt(0));

      var _polyline = new Polyline(PointsToFlatArray(vertices), ModelUnits);
      _polyline.closed = polyline.Closed;
      _polyline.length = polyline.Length;
      _polyline.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return _polyline;
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
        if (polyline.Closed)
          vertices.Add(vertices[0]);
      }

      var _polyline = new Polyline(PointsToFlatArray(vertices), ModelUnits);
      _polyline.closed = polyline.Closed;
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
      for (int i = 0; i < exploded.Count; i++)
        segments.Add((ICurve)ConvertToSpeckle(exploded[i]));
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
      for (int i = 0; i < polyline.NumberOfVertices; i++)
      {
        SegmentType type = polyline.GetSegmentType(i);
        switch (type)
        {
          case SegmentType.Line:
            segments.Add(LineToSpeckle(polyline.GetLineSegmentAt(i)));
            break;
          case SegmentType.Arc:
            segments.Add(ArcToSpeckle(polyline.GetArcSegmentAt(i)));
            break;
        }
      }
      polycurve.segments = segments;

      polycurve.length = polyline.Length;
      polycurve.bbox = BoxToSpeckle(polyline.GeometricExtents, true);

      return polycurve;
    }

    // polylines can only support curve segments of type circular arc
    // currently, this will collapse 3d polycurves into 2d since there is no polycurve class that can contain 3d polylines with nonlinear segments
    // TODO: to preserve 3d polycurves, will have to convert segments individually, append to the document, and join. This will convert to spline if 3d with curved segments.
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
            throw new Speckle.Core.Logging.SpeckleException("Polycurve segment is not a line or arc!");
        }
      }

      return polyline;
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
      return AcadDB.Curve.CreateFromGeCurve(NurbcurveToNative(curve));
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

    // Surfaces
    public Surface SurfaceToSpeckle(AcadDB.PlaneSurface surface)
    {
      var nurbs = surface.ConvertToNurbSurface();
      if (nurbs.Length > 0)
        return SurfaceToSpeckle(nurbs[0]);
      return null;
    }

    public Surface SurfaceToSpeckle(AcadDB.NurbSurface surface)
    {
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

    // Breps
    public List<AcadDB.Surface> BrepToNativeDB(Brep brep)
    {
      return brep.Surfaces.Select(o => SurfaceToNativeDB(o)).ToList();
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
    public Mesh MeshToSpeckle(AcadDB.PolyFaceMesh mesh)
    {
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

      var speckleMesh = new Mesh(vertices, faces, colors.ToArray(), null, ModelUnits);
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
      Point3d[] points = PointListToNative(mesh.vertices, mesh.units);

      PolyFaceMesh _mesh = null;
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        _mesh = new PolyFaceMesh();

        // append mesh to blocktable record - necessary before adding vertices and faces
        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite);
        btr.AppendEntity(_mesh);
        tr.AddNewlyCreatedDBObject(_mesh, true);

        // add polyfacemesh vertices
        for (int i = 0; i < points.Length; i++)
        {
          var vertex = new PolyFaceMeshVertex(points[i]);
          try
          {
            Color color = Color.FromArgb(mesh.colors[i]);
            vertex.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(color.R, color.G, color.B);
          }
          catch { }
          _mesh.AppendVertex(vertex);
          tr.AddNewlyCreatedDBObject(vertex, true);
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
            _mesh.AppendFaceRecord(face);
            tr.AddNewlyCreatedDBObject(face, true);
          }
        }

        tr.Commit();
      }
      
      return _mesh;
    }

    public BlockInstance BlockReferenceToSpeckle(AcadDB.BlockReference reference)
    {
      // get record
      BlockDefinition definition = null;
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
        definition = BlockRecordToSpeckle(btr);
        tr.Commit();
      }

      var instance = new BlockInstance()
      {
        insertionPoint = PointToSpeckle(reference.Position),
        transform = reference.BlockTransform.ToArray(),
        blockDefinition = definition,
        units = ModelUnits
      };

      return instance;
    }
    public string BlockInstanceToNativeDB( BlockInstance instance)
    {
      string result = null;

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

      
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        BlockTable blckTbl = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        BlockTableRecord modelSpaceRecord = (BlockTableRecord)tr.GetObject(blckTbl[BlockTableRecord.ModelSpace], AcadDB.OpenMode.ForWrite);

        BlockReference br = new BlockReference(insertionPoint, definitionId);
        br.BlockTransform = convertedTransform;
        modelSpaceRecord.AppendEntity(br);
        tr.AddNewlyCreatedDBObject(br, true);
        result = "success";

        tr.Commit();
      }

      return result;
    }

    public BlockDefinition BlockRecordToSpeckle (BlockTableRecord record)
    {
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
      var blockName = $"{Doc.UserData["commit"]} - {definition.name}";

      ObjectId blockId = ObjectId.Null;
      using (Transaction tr = Doc.TransactionManager.StartTransaction())
      {
        // see if block record already exists and return if so
        BlockTable blckTbl = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        foreach (ObjectId id in blckTbl)
        {
          BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
          if (btr.Name == blockName)
          {
            tr.Commit();
            return id;
          }
        }

        // create btr
        using (BlockTableRecord btr = new BlockTableRecord())
        {
          btr.Name = blockName;

          // base point
          btr.Origin = PointToNative(definition.basePoint);

          // add geometry
          blckTbl.UpgradeOpen();
          foreach (var geo in definition.geometry)
          {
            if (CanConvertToNative(geo))
            {
              var converted = ConvertToNative(geo) as Entity;
              if (converted == null)
                continue;
              btr.AppendEntity(converted);
            }
          }
          blockId = blckTbl.Add(btr);
          tr.AddNewlyCreatedDBObject(btr, true);
        }
        tr.Commit();
      }

      return blockId;
    }
  }
}

#if (CIVIL2021 || CIVIL2022)
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilDB = Autodesk.Civil.DatabaseServices;
using Civil = Autodesk.Civil;
using Autodesk.AutoCAD.Geometry;
using Acad = Autodesk.AutoCAD.Geometry;

using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using Interval = Objects.Primitive.Interval;
using Polycurve = Objects.Geometry.Polycurve;
using Curve = Objects.Geometry.Curve;
using Featureline = Objects.BuiltElements.Featureline;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Brep = Objects.Geometry.Brep;
using Mesh = Objects.Geometry.Mesh;
using Pipe = Objects.BuiltElements.Pipe;
using Plane = Objects.Geometry.Plane;
using Polyline = Objects.Geometry.Polyline;
using Spiral = Objects.Geometry.Spiral;
using SpiralType = Objects.Geometry.SpiralType;
using Station = Objects.BuiltElements.Station;
using Structure = Objects.BuiltElements.Structure;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // stations
    public Station StationToSpeckle(CivilDB.Station station)
    {
      var _station = new Station();
      _station.location = PointToSpeckle(station.Location);
      _station.type = station.StationType.ToString();
      _station.number = station.RawStation;
      _station.units = ModelUnits;

      return _station;
    }

    // alignments
    public SpiralType SpiralTypeToSpeckle(Civil.SpiralType type)
    {
      switch (type)
      {
        case Civil.SpiralType.Clothoid:
          return SpiralType.Clothoid;
        case Civil.SpiralType.Bloss:
          return SpiralType.Bloss;
        case Civil.SpiralType.BiQuadratic:
          return SpiralType.Biquadratic;
        case Civil.SpiralType.CubicParabola:
          return SpiralType.CubicParabola;
        case Civil.SpiralType.Sinusoidal:
          return SpiralType.Sinusoid;
        default:
          return SpiralType.Unknown;
      }
    }

    public Civil.SpiralType SpiralTypeToNative(SpiralType type)
    {
      switch (type)
      {
        case SpiralType.Clothoid:
          return Civil.SpiralType.Clothoid;
        case SpiralType.Bloss:
          return Civil.SpiralType.Bloss;
        case SpiralType.Biquadratic:
          return Civil.SpiralType.BiQuadratic;
        case SpiralType.CubicParabola:
          return Civil.SpiralType.CubicParabola;
        case SpiralType.Sinusoid:
          return Civil.SpiralType.Sinusoidal;
        default:
          return Civil.SpiralType.Clothoid;
      }
    }
    public Alignment AlignmentToSpeckle(CivilDB.Alignment alignment)
    {
      var _alignment = new Alignment();

      // get the alignment subentity curves
      List<ICurve> curves = new List<ICurve>();
      var stations = new List<double>();
      for (int i = 0; i < alignment.Entities.Count; i++)
      {
        var entity = alignment.Entities.GetEntityByOrder(i);

        var polycurve = new Polycurve(units: ModelUnits, applicationId: entity.EntityId.ToString());
        var segments = new List<ICurve>();
        double length = 0;
        for (int j = 0; j < entity.SubEntityCount; j++)
        {
          CivilDB.AlignmentSubEntity subEntity = entity[j];
          ICurve segment = null;
          switch (subEntity.SubEntityType)
          {
            case CivilDB.AlignmentSubEntityType.Arc:
              var arc = subEntity as CivilDB.AlignmentSubEntityArc;
              segment = AlignmentArcToSpeckle(arc);
              break;
            case CivilDB.AlignmentSubEntityType.Line:
              var line = subEntity as CivilDB.AlignmentSubEntityLine;
              segment = AlignmentLineToSpeckle(line);
              break;
            case CivilDB.AlignmentSubEntityType.Spiral:
              var spiral = subEntity as CivilDB.AlignmentSubEntitySpiral;
              segment = AlignmentSpiralToSpeckle(spiral, alignment);
              break;
          }
          if (segment != null)
          {
            length += subEntity.Length;
            segments.Add(segment);
          }
        }
        if (segments.Count == 1)
        {
          curves.Add(segments[0]);
        }
        else
        {
          polycurve.segments = segments;
          polycurve.length = length;

          // add additional props like entity type
          polycurve["alignmentType"] = entity.EntityType.ToString();
          curves.Add(polycurve);
        }
      }

      // get display poly
      var poly = alignment.BaseCurve as Autodesk.AutoCAD.DatabaseServices.Polyline;
      using (Polyline2d poly2d = poly.ConvertTo(false))
      {
        _alignment.displayValue = CurveToSpeckle(poly2d.Spline.ToPolyline()) as Polyline;
      }

      _alignment.curves = curves;
      if (alignment.DisplayName != null)
        _alignment.name = alignment.DisplayName;
      if (alignment.StartingStation != null)
        _alignment.startStation = alignment.StartingStation;
      if (alignment.EndingStation != null)
        _alignment.endStation = alignment.EndingStation;

      // handle station equations
      var equations = new List<double>();
      var directions = new List<bool>();
      foreach (var stationEquation in alignment.StationEquations)
      {
        equations.AddRange(new List<double> { stationEquation.RawStationBack, stationEquation.StationBack, stationEquation.StationAhead });
        bool equationIncreasing = (stationEquation.EquationType.Equals(CivilDB.StationEquationType.Increasing)) ? true : false;
        directions.Add(equationIncreasing);
      }
      _alignment.stationEquations = equations;
      _alignment.stationEquationDirections = directions;

      _alignment.units = ModelUnits;

      // add civil3d props if they exist
      if (alignment.SiteName != null)
        _alignment["site"] = alignment.SiteName;
      if (alignment.StyleName != null)
        _alignment["style"] = alignment.StyleName;

      return _alignment;
    }
    public CivilDB.Alignment AlignmentToNative(Alignment alignment)
    {
      var name = alignment.name ?? string.Empty;
      var layer = Doc.Database.LayerZero;

      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();
      var civilDoc = CivilApplication.ActiveDocument;
      if (civilDoc == null)
        return null;

#region properties
      var site = ObjectId.Null;
      var style = civilDoc.Styles.AlignmentStyles.First();
      var label = civilDoc.Styles.LabelSetStyles.AlignmentLabelSetStyles.First();

      // get site
      if (alignment["site"] != null)
      {
        var _site = alignment["site"] as string;
        if (_site != string.Empty)
        {
          foreach (ObjectId docSite in civilDoc.GetSiteIds())
          {
            var siteEntity = Trans.GetObject(docSite, OpenMode.ForRead) as CivilDB.Site;
            if (siteEntity.Name.Equals(_site))
            {
              site = docSite;
              break;
            }
          }
        }
      }

      // get style
      if (alignment["style"] != null)
      {
        var _style = alignment["style"] as string;
        foreach (var docStyle in civilDoc.Styles.AlignmentStyles)
        {
          var styleEntity = Trans.GetObject(docStyle, OpenMode.ForRead) as CivilDB.Styles.AlignmentStyle;
          if (styleEntity.Name.Equals(_style))
          {
            style = docStyle;
            break;
          }
        }
      }

      // get labelset
      if (alignment["label"] != null)
      {
        var _label = alignment["label"] as string;
        foreach (var docLabelSet in civilDoc.Styles.LabelSetStyles.AlignmentLabelSetStyles)
        {
          var labelEntity = Trans.GetObject(docLabelSet, OpenMode.ForRead) as CivilDB.Styles.AlignmentLabelSetStyle;
          if (labelEntity.Name.Equals(_label))
          {
            label = docLabelSet;
            break;
          }
        }
      }
#endregion

      // create alignment entity curves
      var id = CivilDB.Alignment.Create(civilDoc, name, site, layer, style, label);
      if (id == ObjectId.Null)
        return null;
      var _alignment = Trans.GetObject(id, OpenMode.ForWrite) as CivilDB.Alignment;
      var entities = _alignment.Entities;
      foreach (var curve in alignment.curves)
        AddAlignmentEntity(curve, ref entities);

      return _alignment;
    }

#region helper methods
    private void AddAlignmentEntity(ICurve curve, ref CivilDB.AlignmentEntityCollection entities)
    {
      switch (curve)
      {
        case Line o:
          entities.AddFixedLine(PointToNative(o.start), PointToNative(o.end));
          break;

        case Arc o:
          entities.AddFixedCurve(entities.LastEntity, PointToNative(o.startPoint), PointToNative(o.midPoint), PointToNative(o.endPoint));
          break;

        case Spiral o:
          var start = PointToNative(o.startPoint);
          var end = PointToNative(o.endPoint);
          var intersectionPoints = o.displayValue.GetPoints(); // display poly points should be points of intersection for the spiral
          if (intersectionPoints.Count == 0 )
            break;
          var intersectionPoint = PointToNative(intersectionPoints[intersectionPoints.Count / 2]); 
          entities.AddFixedSpiral(entities.LastEntity, start, intersectionPoint , end, SpiralTypeToNative(o.spiralType));
          break;

        case Polycurve o:
          foreach (var segment in o.segments)
            AddAlignmentEntity(segment, ref entities);
          break;

        default:
          break;
      }
    }
    private Line AlignmentLineToSpeckle(CivilDB.AlignmentSubEntityLine line)
    {
      var _line = LineToSpeckle(new LineSegment2d(line.StartPoint, line.EndPoint));
      return _line;
    }
    private Arc AlignmentArcToSpeckle(CivilDB.AlignmentSubEntityArc arc)
    {
      // calculate midpoint of chord as between start and end point
      Point2d chordMid = new Point2d((arc.StartPoint.X + arc.EndPoint.X) / 2, (arc.StartPoint.Y + arc.EndPoint.Y) / 2);

      // calculate sagitta as radius minus distance between arc center and chord midpoint
      var sagitta = arc.Radius - arc.CenterPoint.GetDistanceTo(chordMid);

      // get unit vector from arc center to chord mid
      var midVector = arc.CenterPoint.GetVectorTo(chordMid);
      var unitMidVector = midVector.DivideBy(midVector.Length);

      // get midpoint of arc by moving chord mid point the length of the sagitta along mid vector
      var midPoint = chordMid.Add(unitMidVector.MultiplyBy(sagitta));

      // find arc plane (normal is in clockwise dir)
      var center3 = new Point3d(arc.CenterPoint.X, arc.CenterPoint.Y, 0);
      Acad.Plane plane = (arc.Clockwise) ? new Acad.Plane(center3, Vector3d.ZAxis.MultiplyBy(-1)) : new Acad.Plane(center3, Vector3d.ZAxis);

      // calculate start and end angles
      var startVector = new Vector3d(arc.StartPoint.X - center3.X, arc.StartPoint.Y - center3.Y, 0);
      var endVector = new Vector3d(arc.EndPoint.X - center3.X, arc.EndPoint.Y - center3.Y, 0);
      var startAngle = startVector.AngleOnPlane(plane);
      var endAngle = endVector.AngleOnPlane(plane);

      // calculate total angle. 
      // TODO: This needs to be improved with more research into autocad .AngleOnPlane() return values (negative angles, etc).
      var totalAngle = (arc.Clockwise) ? System.Math.Abs(endAngle - startAngle) : System.Math.Abs(endAngle - startAngle);

      // create arc
      var _arc = new Arc(PlaneToSpeckle(plane), arc.Radius, startAngle, endAngle, totalAngle, ModelUnits);
      _arc.startPoint = PointToSpeckle(arc.StartPoint);
      _arc.endPoint = PointToSpeckle(arc.EndPoint);
      _arc.midPoint = PointToSpeckle(midPoint);
      _arc.domain = IntervalToSpeckle(new Acad.Interval(0, 1, tolerance));
      _arc.length = arc.Length;

      return _arc;
    }
    
    private Spiral AlignmentSpiralToSpeckle(CivilDB.AlignmentSubEntitySpiral spiral, CivilDB.Alignment alignment)
    {
      var _spiral = new Spiral();
      _spiral.startPoint = PointToSpeckle(spiral.StartPoint);
      _spiral.endPoint = PointToSpeckle(spiral.EndPoint);
      _spiral.length = spiral.Length;
      _spiral.pitch = 0;
      _spiral.spiralType = SpiralTypeToSpeckle(spiral.SpiralDefinition);

      // get plane
      var vX = new Vector3d(System.Math.Cos(spiral.StartDirection) + spiral.StartPoint.X, System.Math.Sin(spiral.StartDirection) + spiral.StartPoint.Y, 0);
      var vY = vX.RotateBy(System.Math.PI / 2, Vector3d.ZAxis);
      var plane = new Acad.Plane(new Point3d(spiral.RadialPoint.X, spiral.RadialPoint.Y, 0), vX, vY);
      _spiral.plane = PlaneToSpeckle(plane);

      // get turns
      int turnDirection = (spiral.Direction == CivilDB.SpiralDirectionType.DirectionLeft) ? 1 : -1;
      _spiral.turns = turnDirection * spiral.Delta / (System.Math.PI * 2);

      // create polyline display, default tessellation length is 1
      var tessellation = 1;
      int spiralSegmentCount = System.Convert.ToInt32(System.Math.Ceiling(spiral.Length / tessellation));
      spiralSegmentCount = (spiralSegmentCount < 10) ? 10 : spiralSegmentCount;
      double spiralSegmentLength = spiral.Length / spiralSegmentCount;
      List<Point2d> points = new List<Point2d>();
      points.Add(spiral.StartPoint);
      for (int i = 1; i < spiralSegmentCount; i++)
      {
        double x = 0;
        double y = 0;
        double z = 0;

        alignment.PointLocation(spiral.StartStation + (i * spiralSegmentLength), 0, tolerance, ref x, ref y, ref z);
        points.Add(new Point2d(x, y));
      }
      points.Add(spiral.EndPoint);
      double length = 0;
      for (int j = 1; j < points.Count; j++)
      {
        length += points[j].GetDistanceTo(points[j - 1]);
      }
      var poly = new Polyline();
      poly.value = PointsToFlatList(points);
      poly.units = ModelUnits;
      poly.closed = (spiral.StartPoint != spiral.EndPoint) ? false : true;
      poly.length = length;
      _spiral.displayValue = poly;

      return _spiral;
    }
                        
#endregion
     
    // profiles
    public Base ProfileToSpeckle(CivilDB.Profile profile)
    {
      var curve = CurveToSpeckle(profile.BaseCurve, ModelUnits) as Base;

      if (profile.DisplayName != null)
        curve["name"] = profile.DisplayName;
      if (profile.Description != null)
        curve["description"] = profile.Description;
      if (profile.StartingStation != null)
        curve["startStation"] = profile.StartingStation;
      if (profile.EndingStation != null)
        curve["endStation"] = profile.EndingStation;
      curve["profileType"] = profile.ProfileType.ToString();
      curve["offset"] = profile.Offset;
      curve["units"] = ModelUnits;

      return curve;
    }

    // featurelines
    public Featureline FeatureLineToSpeckle(CivilDB.FeatureLine featureline)
    {
      var _featureline = new Featureline();

      _featureline.baseCurve = CurveToSpeckle(featureline.BaseCurve, ModelUnits);
      _featureline.name = (featureline.DisplayName != null) ? featureline.DisplayName : "";
      _featureline["description"] = (featureline.Description != null) ? featureline.Description : "";
      _featureline.units = ModelUnits;

      List<Point> piPoints = new List<Point>();
      List<Point> elevationPoints = new List<Point>();
      
      foreach (Autodesk.AutoCAD.Geometry.Point3d point in featureline.GetPoints(Autodesk.Civil.FeatureLinePointType.PIPoint))
        piPoints.Add(PointToSpeckle(point));
      foreach (Autodesk.AutoCAD.Geometry.Point3d point in featureline.GetPoints(Autodesk.Civil.FeatureLinePointType.ElevationPoint))
        elevationPoints.Add(PointToSpeckle(point));
      if (piPoints.Count > 0)
        _featureline[@"piPoints"] = piPoints;
      if (elevationPoints.Count > 0)
        _featureline[@"elevationPoints"] = elevationPoints;

      try { _featureline["site"] = featureline.SiteId.ToString(); } catch { }

      return _featureline;
    }

    public CivilDB.FeatureLine FeatureLineToNative(Polycurve polycurve)
    {
      return null;
    }

    // surfaces
    public Mesh SurfaceToSpeckle(CivilDB.TinSurface surface)
    {
      Mesh mesh = null;

      // output vars
      var _vertices = new List<Acad.Point3d>();
      var faces = new List<int>();

      foreach (var triangle in surface.GetTriangles(false))
      {
        // get vertices
        var faceIndices = new List<int>();
        foreach (var vertex in new List<CivilDB.TinSurfaceVertex>() {triangle.Vertex1, triangle.Vertex2, triangle.Vertex3})
        {
          if (!_vertices.Contains(vertex.Location))
          {
            faceIndices.Add(_vertices.Count);
            _vertices.Add(vertex.Location);
          }
          else
          {
            faceIndices.Add(_vertices.IndexOf(vertex.Location));
          }
          vertex.Dispose();
        }

        // get face
        faces.AddRange(new List<int> { 0, faceIndices[0], faceIndices[1], faceIndices[2] });

        triangle.Dispose();
      }

      var vertices = PointsToFlatList(_vertices);
      mesh = new Mesh(vertices, faces);
      mesh.units = ModelUnits;
      mesh.bbox = BoxToSpeckle(surface.GeometricExtents);

      // add tin surface props
      try{
      mesh["name"] = surface.DisplayName;
      mesh["description"] = surface.Description;
      }
      catch{}

      return mesh;
    }

    public Mesh SurfaceToSpeckle(CivilDB.GridSurface surface)
    {
      Mesh mesh = null;

      // output vars
      var _vertices = new List<Acad.Point3d>();
      var faces = new List<int>();

      foreach (var cell in surface.GetCells(false))
      {
        // get vertices
        var faceIndices = new List<int>();
        foreach (var vertex in new List<CivilDB.GridSurfaceVertex>() {cell.BottomLeftVertex, cell.BottomRightVertex, cell.TopLeftVertex, cell.TopRightVertex})
        {
          if (!_vertices.Contains(vertex.Location))
          {
            faceIndices.Add(_vertices.Count);
            _vertices.Add(vertex.Location);
          }
          else
          {
            faceIndices.Add(_vertices.IndexOf(vertex.Location));
          }
          vertex.Dispose();
        }

        // get face
        faces.AddRange(new List<int> { 1, faceIndices[0], faceIndices[1], faceIndices[2], faceIndices[3] });

        cell.Dispose();
      }

      var vertices = PointsToFlatList(_vertices);
      mesh = new Mesh(vertices, faces);
      mesh.units = ModelUnits;
      mesh.bbox = BoxToSpeckle(surface.GeometricExtents);

      // add grid surface props
      try{
      mesh["name"] = surface.DisplayName;
      mesh["description"] = surface.Description;
      }
      catch{}

      return mesh;
    }

    // structures
    public Structure StructureToSpeckle(CivilDB.Structure structure)
    {
      // get ids pipes that are connected to this structure
      var pipeIds = new List<string>();
      for (int i = 0; i < structure.ConnectedPipesCount; i++)
        pipeIds.Add(structure.get_ConnectedPipe(i).ToString());

      var _structure = new Structure();

      _structure.location = PointToSpeckle(structure.Location, ModelUnits);
      _structure.pipeIds = pipeIds;
      _structure.displayMesh = SolidToSpeckle(structure.Solid3dBody);
      _structure.units = ModelUnits;

      // assign additional structure props
      _structure["name"] = (structure.DisplayName != null) ? structure.DisplayName : "";
      _structure["description"] = (structure.Description != null) ? structure.Description : "";
      try{ _structure["grate"] = structure.Grate; } catch{ }
      try{ _structure["station"] = structure.Station; } catch{ }
      try{ _structure["network"] = structure.NetworkName; } catch{ }

      return _structure;
    }

    // pipes
    // TODO: add pressurepipes and pressure fittings when they become supported by C3D api
    public Pipe PipeToSpeckle(CivilDB.Pipe pipe)
    {
      // get the pipe curve
      ICurve curve = null;
      switch (pipe.SubEntityType)
      {
        case CivilDB.PipeSubEntityType.Straight:
          var line = new Acad.LineSegment3d(pipe.StartPoint, pipe.EndPoint);
          curve = LineToSpeckle(line);
          break;
        default:
          curve = CurveToSpeckle(pipe.Spline);
          break;
      }

      var _pipe = new Pipe();
      _pipe.baseCurve = curve;
      _pipe.diameter = pipe.InnerDiameterOrWidth;
      _pipe.length = pipe.Length3DToInsideEdge;
      _pipe.displayMesh = SolidToSpeckle(pipe.Solid3dBody);
      _pipe.units = ModelUnits;

      // assign additional structure props
      _pipe["name"] = (pipe.DisplayName != null) ? pipe.DisplayName : "";
      _pipe["description"] = (pipe.DisplayName != null) ? pipe.Description : "";
      try { _pipe["shape"] = pipe.CrossSectionalShape.ToString(); } catch { }
      try { _pipe["slope"] = pipe.Slope; } catch { }
      try { _pipe["flowDirection"] = pipe.FlowDirection.ToString(); } catch { }
      try { _pipe["flowRate"] = pipe.FlowRate; } catch { }
      try { _pipe["network"] = pipe.NetworkName; } catch { }
      try { _pipe["startOffset"] = pipe.StartOffset; } catch { }
      try { _pipe["endOffset"] = pipe.EndOffset; } catch { }
      try { _pipe["startStation"] = pipe.StartStation; } catch { }
      try { _pipe["endStation"] = pipe.EndStation; } catch { }
      try { _pipe["startStructure"] = pipe.StartStructureId.ToString(); } catch { }
      try { _pipe["endStructure"] = pipe.EndStructureId.ToString(); } catch { }

      // add start and end structure ids

      return _pipe;
    }

    // corridors
    // displaymesh: mesh representation corridor solid
    public Base CorridorToSpeckle(CivilDB.Corridor corridor)
    {
      var _corridor = new Base();

      var baselines = new List<Base>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (var baseline in corridor.Baselines)
        {
          Base convertedBaseline = new Base();

          /* this is just for construction, not relevant info
          if (baseline.IsFeatureLineBased()) // featurelines will only be created if assembly has point codes
          {
            var featureline = tr.GetObject(baseline.FeatureLineId, OpenMode.ForRead) as CivilDB.FeatureLine;
            convertedBaseline = FeatureLineToSpeckle(featureline);
          }
          else
          {
            var alignment = tr.GetObject(baseline.AlignmentId, OpenMode.ForRead) as CivilDB.Alignment;
            convertedBaseline = AlignmentToSpeckle(alignment);
          }
          */

          // get the collection of featurelines for this baseline
          var featurelines = new List<Featureline>();
          foreach (var mainFeaturelineCollection in baseline.MainBaselineFeatureLines.FeatureLineCollectionMap) // main featurelines
            foreach (var featureline in mainFeaturelineCollection)
              featurelines.Add(GetCorridorFeatureline(featureline));
          foreach (var offsetFeaturelineCollection in baseline.OffsetBaselineFeatureLinesCol) // offset featurelines
            foreach (var featurelineCollection in offsetFeaturelineCollection.FeatureLineCollectionMap)
              foreach (var featureline in featurelineCollection)
                featurelines.Add(GetCorridorFeatureline(featureline, true));

          convertedBaseline[@"featurelines"] = featurelines;
          convertedBaseline["type"] = baseline.BaselineType.ToString();
          convertedBaseline.applicationId = baseline.baselineGUID.ToString();
          try { convertedBaseline["stations"] = baseline.SortedStations(); } catch { }

          baselines.Add(convertedBaseline);
        }

        tr.Commit();
      }
      
      _corridor["@baselines"] = baselines;
      _corridor["name"] = (corridor.DisplayName != null) ? corridor.DisplayName : "";
      _corridor["description"] = (corridor.Description != null) ? corridor.Description : "";
      _corridor["units"] = ModelUnits;

      return _corridor;
    }

    private Featureline GetCorridorFeatureline(CivilDB.CorridorFeatureLine featureline = null, bool isOffset = false)
    {
      // construct the 3d polyline
      var collection = new Acad.Point3dCollection();
      foreach (var point in featureline.FeatureLinePoints)
        collection.Add(point.XYZ);
      var polyline = new Polyline3d(Poly3dType.SimplePoly, collection, false);

      // create featureline
      var _featureline = new Featureline();
      _featureline.baseCurve = PolylineToSpeckle(polyline);
      _featureline.name = featureline.CodeName;
      _featureline.units = ModelUnits;
      _featureline["isOffset"] = isOffset;

      return _featureline;
    }
  }
}
#endif
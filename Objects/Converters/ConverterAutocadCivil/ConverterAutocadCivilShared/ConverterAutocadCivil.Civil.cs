#if CIVIL2021 || CIVIL2022 || CIVIL2023
using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilDB = Autodesk.Civil.DatabaseServices;
using Civil = Autodesk.Civil;
using Autodesk.AutoCAD.Geometry;
using Acad = Autodesk.AutoCAD.Geometry;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Objects.BuiltElements.Civil;
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
using Profile = Objects.BuiltElements.Profile;
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
    public CivilAlignment AlignmentToSpeckle(CivilDB.Alignment alignment)
    {
      var _alignment = new CivilAlignment();

      // get alignment props
      _alignment.type = alignment.AlignmentType.ToString();
      _alignment.offset = alignment.IsOffsetAlignment ? alignment.OffsetAlignmentInfo.NominalOffset : 0;
      if (alignment.SiteName != null)
        _alignment.site = alignment.SiteName;
      if (alignment.StyleName != null)
        _alignment.style = alignment.StyleName;
      if (alignment.Description != null)
        _alignment["description"] = alignment.Description;
      if (alignment.Name != null)
        _alignment.name = alignment.Name;

      // get alignment stations
      _alignment.startStation = alignment.StartingStation;
      _alignment.endStation = alignment.EndingStation;
      var stations = alignment.GetStationSet(CivilDB.StationTypes.All).ToList();
      List<Station> _stations = stations.Select(o => StationToSpeckle(o)).ToList();
      if (_stations.Count > 0) _alignment["@stations"] = _stations;

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

      // get the alignment subentity curves
      List<ICurve> curves = new List<ICurve>();
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
      _alignment.curves = curves;


      // if offset alignment, also set parent and offset side
      if (alignment.IsOffsetAlignment)
      {
        _alignment["offsetSide"] = alignment.OffsetAlignmentInfo.Side.ToString();
        try
        {
          var parent = Trans.GetObject(alignment.OffsetAlignmentInfo.ParentAlignmentId, OpenMode.ForRead) as CivilDB.Alignment;
          if (parent != null && parent.Name != null)
            _alignment.parent = parent.Name;
        }
        catch { }
      }

      return _alignment;
    }
    public ApplicationObject AlignmentToNative(Alignment alignment)
    {
      var appObj = new ApplicationObject(alignment.id, alignment.speckle_type) { applicationId = alignment.applicationId };
      var existingObjs = GetExistingElementsByApplicationId(alignment.applicationId);
      var civilAlignment = alignment as CivilAlignment;

      // get civil doc
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();
      var civilDoc = CivilApplication.ActiveDocument;
      if (civilDoc == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not retrieve civil3d document");
        return appObj;
      }

      // create or retrieve alignment, and parent if it exists
      CivilDB.Alignment _alignment = existingObjs.Any() ? Trans.GetObject(existingObjs.FirstOrDefault(), OpenMode.ForWrite) as CivilDB.Alignment : null;
      var parent = civilAlignment != null ? GetFromObjectIdCollection(civilAlignment.parent, civilDoc.GetAlignmentIds()) : ObjectId.Null;
      bool isUpdate = true;
      if (_alignment == null || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create) // just create a new alignment
      {
        isUpdate = false;

        // get civil props for creation
#region properties
        var name = string.IsNullOrEmpty(alignment.name) ? alignment.applicationId : alignment.name; // names need to be unique on creation (but not send i guess??)
        var layer = Doc.Database.LayerZero;

        // type
        var type = CivilDB.AlignmentType.Centerline;
        if (civilAlignment != null)
          if (Enum.TryParse(civilAlignment.type, out CivilDB.AlignmentType civilType))
            type = civilType;

        // site
        var site = civilAlignment != null ? 
          GetFromObjectIdCollection(civilAlignment.site, civilDoc.GetSiteIds()) : ObjectId.Null;

        // style
        var docStyles = new ObjectIdCollection();
        foreach (ObjectId styleId in civilDoc.Styles.AlignmentStyles) docStyles.Add(styleId);
        var style = civilAlignment != null ? 
          GetFromObjectIdCollection(civilAlignment.style, docStyles, true) :  civilDoc.Styles.AlignmentStyles.First();

        // label set style
        var labelStyles = new ObjectIdCollection();
        foreach (ObjectId styleId in civilDoc.Styles.LabelSetStyles.AlignmentLabelSetStyles) labelStyles.Add(styleId);
        var label = civilAlignment != null ?
          GetFromObjectIdCollection(civilAlignment["label"] as string, labelStyles, true) : civilDoc.Styles.LabelSetStyles.AlignmentLabelSetStyles.First();
#endregion

        try
        {
          // add new alignment to doc
          // ⚠ this will throw if name is not unique!!
          var id = ObjectId.Null;
          switch (type)
          {
            case CivilDB.AlignmentType.Offset:
              // create only if parent exists in doc
              if (parent == ObjectId.Null) goto default;
              try
              {
                id = CivilDB.Alignment.CreateOffsetAlignment(name, parent, civilAlignment.offset, style);
              }
              catch
              {
                id = CivilDB.Alignment.CreateOffsetAlignment(CivilDB.Alignment.GetNextUniqueName(name), parent, civilAlignment.offset, style);
              }
              break;
            default:
              try
              {
                id = CivilDB.Alignment.Create(civilDoc, name, site, layer, style, label);
              }
              catch
              {
                id = CivilDB.Alignment.Create(civilDoc, CivilDB.Alignment.GetNextUniqueName(name), site, layer, style, label);
              }
              break;
          }
          if (!id.IsValid)
          {
            appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Create method returned null");
            return appObj;
          }
          _alignment = Trans.GetObject(id, OpenMode.ForWrite) as CivilDB.Alignment;
        }
        catch (System.Exception e)
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
          return appObj;
        }
      }

      if (_alignment == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"returned null after bake");
        return appObj;
      }

      if (isUpdate) appObj.Container = _alignment.Layer; // set the appobj container to be the same layer as the existing alignment

      if (parent != ObjectId.Null)
      {
        _alignment.OffsetAlignmentInfo.NominalOffset = civilAlignment.offset; // just update the offset
      }
      else
      {
        // create alignment entity curves
        var entities = _alignment.Entities;
        if (isUpdate) _alignment.Entities.Clear(); // remove existing curves
        foreach (var curve in alignment.curves)
          AddAlignmentEntity(curve, ref entities);
      }

      // set start station
      _alignment.ReferencePointStation = alignment.startStation;

      // update appobj
      var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: status, createdId: _alignment.Handle.ToString(), convertedItem: _alignment);
      return appObj;
    }

    #region helper methods
    private SpiralType SpiralTypeToSpeckle(Civil.SpiralType type)
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
    private Civil.SpiralType SpiralTypeToNative(SpiralType type)
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
      // if greater than 180 >, move in other direction of distance radius + radius - sagitta
      // in the case of an exactly perfect half circle arc...🤷‍♀️
      Point2d midPoint = chordMid.Add(unitMidVector.MultiplyBy(sagitta));
      try
      {
        if (arc.GreaterThan180) // sometimes this prop throws an exception??
          midPoint = chordMid.Add(unitMidVector.Negate().MultiplyBy(2 * arc.Radius - sagitta));
      }
      catch { }

      // create arc
      var _arc = new CircularArc2d(arc.StartPoint, midPoint, arc.EndPoint);
      return ArcToSpeckle(_arc);
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
      poly.value = points.SelectMany(o => PointToSpeckle(o).ToList()).ToList();
      poly.units = ModelUnits;
      poly.closed = (spiral.StartPoint != spiral.EndPoint) ? false : true;
      poly.length = length;
      _spiral.displayValue = poly;

      return _spiral;
    }

#endregion

    // profiles
    public Profile ProfileToSpeckle(CivilDB.Profile profile)
    {
      // get the profile entity curves
      List<ICurve> curves = new List<ICurve>();
      for (int i = 0; i < profile.Entities.Count; i++)
      {
        CivilDB.ProfileEntity entity = profile.Entities[i];
        switch (entity.EntityType)
        {
          case CivilDB.ProfileEntityType.Circular:
            var circular = ProfileArcToSpeckle(entity as CivilDB.ProfileCircular);
            if (circular != null) curves.Add(circular);
            break;
          case CivilDB.ProfileEntityType.Tangent:
            var tangent = ProfileLineToSpeckle(entity as CivilDB.ProfileTangent);
            if (tangent != null) curves.Add(tangent);
            break;
          case CivilDB.ProfileEntityType.ParabolaSymmetric:
          case CivilDB.ProfileEntityType.ParabolaAsymmetric:
          default:
            var segment = ProfileGenericToSpeckle(entity.StartStation, entity.StartElevation, entity.EndStation, entity.EndElevation);
            if (segment != null) curves.Add(segment);
            break;
        }
      }

      // get points of vertical intersection (PVIs)
      List<Point> pvisConverted = new List<Point>();
      var pvis = new Point3dCollection();
      foreach (CivilDB.ProfilePVI pvi in profile.PVIs)
      {
        pvisConverted.Add(PointToSpeckle(new Point2d(pvi.Station, pvi.Elevation)));
        pvis.Add(new Point3d(pvi.Station, pvi.Elevation, 0));
      }

      var _profile = new Profile();
      _profile.name = profile.Name;
      _profile.curves = curves;
      _profile.startStation = profile.StartingStation;
      _profile.endStation = profile.EndingStation;
      if (pvisConverted.Count > 1) _profile.displayValue = PolylineToSpeckle(pvis, profile.Closed);
      _profile.units = ModelUnits;

      // add civil3d props if they exist
      if (profile.StyleName != null)
        _profile["style"] = profile.StyleName;
      if (profile.Description != null)
        _profile["description"] = profile.Description;
      _profile["type"] = profile.ProfileType.ToString();
      if (pvisConverted.Count > 0) _profile["pvis"] = pvisConverted;
      try { _profile["offset"] = profile.Offset; } catch { }

      return _profile;
    }
    private Line ProfileLineToSpeckle(CivilDB.ProfileTangent tangent)
    {
      var start = new Point2d(tangent.StartStation, tangent.StartElevation);
      var end = new Point2d(tangent.EndStation, tangent.EndElevation);
      return LineToSpeckle(new LineSegment2d(start, end));
    }
    private Arc ProfileArcToSpeckle(CivilDB.ProfileCircular circular)
    {
      var start = new Point2d(circular.StartStation, circular.StartElevation);
      var end = new Point2d(circular.EndStation, circular.EndElevation);
      var pvi = new Point2d(circular.PVIStation, circular.PVIElevation);
      return ArcToSpeckle(new CircularArc2d(start, pvi, end));
    }
    private Line ProfileGenericToSpeckle(double startStation, double startElevation, double endStation, double endElevation) // general approximation of segment as line
    {
      var start = new Point2d(startStation, startElevation);
      var end = new Point2d(endStation, endElevation);
      return LineToSpeckle(new LineSegment2d(start, end));
    }
   

    // featurelines
    public Featureline FeatureLineToSpeckle(CivilDB.FeatureLine featureline)
    {
      // get all points
      List<Point3d> points = new List<Point3d>();
      var _points = featureline.GetPoints(Civil.FeatureLinePointType.AllPoints);
      foreach (Point3d point in _points) points.Add(point);

      // get elevation points
      List<Point3d> ePoints = new List<Point3d>();
      var _ePoints = featureline.GetPoints(Civil.FeatureLinePointType.ElevationPoint);
      foreach (Point3d ePoint in _ePoints) ePoints.Add(ePoint);

      // get pi points and indices in all points list
      List<Point3d> piPoints = new List<Point3d>();
      var _piPoints = featureline.GetPoints(Civil.FeatureLinePointType.PIPoint);
      foreach (Point3d piPoint in _piPoints) piPoints.Add(piPoint);
      List<int> indices = piPoints.Select(o => points.IndexOf(o)).ToList();
      
      /*
      // get bulges at pi point indices
      int count = (featureline.Closed) ? featureline.PointsCount : featureline.PointsCount - 1;
      List<double> bulges = new List<double>();
      for (int i = 0; i < count; i++) bulges.Add(featureline.GetBulge(i));
      var piBulges = new List<double>();
      foreach (var index in indices) piBulges.Add(bulges[index]);
      */

      // create 3d poly
      var polyline = new Polyline3d(Poly3dType.SimplePoly, _piPoints, false);

      // featureline
      var _featureline = new Featureline();

      _featureline.displayValue = PolylineToSpeckle(polyline);
      _featureline.curve = CurveToSpeckle(featureline.BaseCurve, ModelUnits);
      _featureline.name = (featureline.Name != null) ? featureline.Name : "";
      _featureline["description"] = (featureline.Description != null) ? featureline.Description : "";
      _featureline.units = ModelUnits;
      try { _featureline["site"] = featureline.SiteId.ToString(); } catch { }

      List<Point> piPointsConverted = piPoints.Select(o => PointToSpeckle(o)).ToList();
      _featureline["@piPoints"] = piPointsConverted;
      List<Point> ePointsConverted = ePoints.Select(o => PointToSpeckle(o)).ToList();
      _featureline["@elevationPoints"] = ePointsConverted;

      return _featureline;
    }
    private Featureline FeaturelineToSpeckle(CivilDB.CorridorFeatureLine featureline)
    {
      // construct the 3d polyline
      var collection = new Acad.Point3dCollection();
      foreach (var point in featureline.FeatureLinePoints)
        collection.Add(point.XYZ);
      var polyline = new Polyline3d(Poly3dType.SimplePoly, collection, false);

      // create featureline
      var _featureline = new Featureline();
      _featureline.curve = PolylineToSpeckle(polyline);
      _featureline.name = featureline.CodeName;
      _featureline.units = ModelUnits;

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
        var triangleVertices = new List<Point3d> { triangle.Vertex1.Location, triangle.Vertex2.Location, triangle.Vertex3.Location };

#if CIVIL2023 // skip any triangles that are hidden in the surface!
        if (!triangle.IsVisible)
        {
          triangle.Dispose();
          continue;
        }
#endif

        // store vertices
        var faceIndices = new List<int>();
        foreach (var vertex in triangleVertices)
        {
          if (!_vertices.Contains(vertex))
          {
            faceIndices.Add(_vertices.Count);
            _vertices.Add(vertex);
          }
          else
          {
            faceIndices.Add(_vertices.IndexOf(vertex));
          }
        }

        // get face
        faces.AddRange(new List<int> { 3, faceIndices[0], faceIndices[1], faceIndices[2] });

        triangle.Dispose();
      }

      var vertices = _vertices.SelectMany(o => PointToSpeckle(o).ToList()).ToList();

      mesh = new Mesh(vertices, faces);
      mesh.units = ModelUnits;
      mesh.bbox = BoxToSpeckle(surface.GeometricExtents);

      // add tin surface props
      var props = Speckle.Core.Models.Utilities.GetApplicationProps(surface, typeof(CivilDB.TinSurface), false);
      mesh[CivilPropName] = props;

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
        faces.AddRange(new List<int> { 4, faceIndices[0], faceIndices[1], faceIndices[2], faceIndices[3] });

        cell.Dispose();
      }

      var vertices = _vertices.Select(o => PointToSpeckle(o).ToList()).SelectMany(o => o).ToList();
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

    public object CivilSurfaceToNative(Mesh mesh)
    {
      var props = mesh[CivilPropName] as Base;
      if (props == null) return null;

      switch (props["class"] as string)
      {
        case "TinSurface":
          return TinSurfaceToNative(mesh, props);
        default:
          return MeshToNativeDB(mesh);
      }
    }
    public ApplicationObject TinSurfaceToNative(Mesh mesh, Base props)
    {
      var appObj = new ApplicationObject(mesh.id, mesh.speckle_type) { applicationId = mesh.applicationId };
      var existingObjs = GetExistingElementsByApplicationId(mesh.applicationId);

      // get civil doc
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();
      var civilDoc = CivilApplication.ActiveDocument;
      if (civilDoc == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not retrieve civil3d document");
        return appObj;
      }

      // create or retrieve tin surface
      CivilDB.TinSurface _surface = existingObjs.Any() ? Trans.GetObject(existingObjs.FirstOrDefault(), OpenMode.ForWrite) as CivilDB.TinSurface : null;
      bool isUpdate = true;
      if (_surface == null || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create) // just create a new surface
      {
        isUpdate = false;

        // get civil props for creation
        var name = string.IsNullOrEmpty(props["Name"] as string) ? mesh.applicationId : props["Name"] as string;
        var layer = Doc.Database.LayerZero;
        var docStyles = new ObjectIdCollection();
        foreach (ObjectId styleId in civilDoc.Styles.SurfaceStyles) docStyles.Add(styleId);
        var style = props["style"] as string != null ?
          GetFromObjectIdCollection(props["style"] as string, docStyles) : civilDoc.Styles.SurfaceStyles.First();

        // add new surface to doc
        // ⚠ this will throw if name is empty?
        var id = ObjectId.Null;
        try
        {
          id = CivilDB.TinSurface.Create(name, style);
        }
        catch (System.Exception e)
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
          return appObj;
        }
        if (!id.IsValid)
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Create method returned null");
          return appObj;
        }
        _surface = Trans.GetObject(id, OpenMode.ForWrite) as CivilDB.TinSurface;
      }

      if (_surface == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"retrieved or baked surface was null");
        return appObj;
      }

      if (isUpdate)
      {
        appObj.Container = _surface.Layer; // set the appobj container to be the same layer as the existing alignment
        _surface.DeleteVertices(_surface.Vertices); // remove existing vertices
      }

      // add all vertices
      var vertices = new Point3dCollection();
      var meshVertices = mesh.GetPoints().Select(o => PointToNative(o)).ToList();
      meshVertices.ForEach(o => vertices.Add(o));
      _surface.AddVertices(vertices);
      

      // loop through faces to create an edge dictionary by vertex, which includes all other vertices this vertex is connected to
      int i = 0;
      var edges = new Dictionary<Point3d, List<Point3d>>();
      foreach (var vertex in meshVertices)
        edges.Add(vertex, new List<Point3d>());
        
      while (i < mesh.faces.Count)
      {
        if (mesh.faces[i] == 3) // triangle
        {
          var v1 = meshVertices[mesh.faces[i + 1]];
          var v2 = meshVertices[mesh.faces[i + 2]];
          var v3 = meshVertices[mesh.faces[i + 3]];
          edges[v1].Add(v2);
          edges[v2].Add(v3);
          edges[v3].Add(v1);

          i += 4;
        }
        else // this was not a triangulated surface! return null
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Mesh was not triangulated");
          return appObj;
        }
      }

      // loop through each surface vertex edge and create any that don't exist 
      foreach (Point3d edgeStart in edges.Keys)
      {
        var vertex = _surface.FindVertexAtXY(edgeStart.X, edgeStart.Y);
        var correctEdges = new List<Point3d>();
        foreach (CivilDB.TinSurfaceEdge currentEdge in vertex.Edges)
        {
          if (edges[edgeStart].Contains(currentEdge.Vertex2.Location))
            correctEdges.Add(currentEdge.Vertex2.Location);
          currentEdge.Dispose();
        }
        vertex.Dispose();

        foreach (var vertexToAdd in edges[edgeStart]) 
        {
          if (correctEdges.Contains(vertexToAdd)) continue;
          var a1 = _surface.FindVertexAtXY(edgeStart.X, edgeStart.Y);
          var a2 = _surface.FindVertexAtXY(vertexToAdd.X, vertexToAdd.Y);
          _surface.AddLine(a1, a2);
          a1.Dispose();
          a2.Dispose();
        }
      }
      

      // loop through and delete any edges
      var edgesToDelete = new List<CivilDB.TinSurfaceEdge>();
      foreach(CivilDB.TinSurfaceVertex vertex in _surface.Vertices)
      {
        if (vertex.Edges.Count > edges[vertex.Location].Count)
          foreach (CivilDB.TinSurfaceEdge modifiedEdge in vertex.Edges)
            if (!edges[vertex.Location].Contains(modifiedEdge.Vertex2.Location) && !edges[modifiedEdge.Vertex2.Location].Contains(vertex.Location))
              edgesToDelete.Add(modifiedEdge);
        vertex.Dispose();
      }
      if (edgesToDelete.Count > 0)
      {
        _surface.DeleteLines(edgesToDelete);
        _surface.Rebuild();
      }
      
      // update appobj
      var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: status, createdId: _surface.Handle.ToString(), convertedItem: _surface);
      return appObj;
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
      _structure.displayValue = new List<Mesh>() { SolidToSpeckle(structure.Solid3dBody, out List<string> notes) };
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
    // TODO: add pressure fittings
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
          curve = CurveToSpeckle(pipe.BaseCurve);
          break;
      }

      var _pipe = new Pipe();
      _pipe.baseCurve = curve;
      _pipe.diameter = pipe.InnerDiameterOrWidth;
      _pipe.length = pipe.Length3DToInsideEdge;
      _pipe.displayValue = new List<Mesh> { SolidToSpeckle(pipe.Solid3dBody, out List<string> notes) };
      _pipe.units = ModelUnits;

      // assign additional pipe props
      if (pipe.Name != null) _pipe["name"] = pipe.Name;
      if (pipe.Description != null) _pipe["description"] = pipe.Description;
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

      return _pipe;
    }
    public Pipe PipeToSpeckle(CivilDB.PressurePipe pipe)
    {
      // get the pipe curve
      ICurve curve = null;
      switch (pipe.BaseCurve)
      {
        case AcadDB.Line o:
          var line = new Acad.LineSegment3d(pipe.StartPoint, pipe.EndPoint);
          curve = LineToSpeckle(line);
          break;
        default:
          curve = CurveToSpeckle(pipe.BaseCurve);
          break;
      }

      var _pipe = new Pipe();
      _pipe.baseCurve = curve;
      _pipe.diameter = pipe.InnerDiameter;
      _pipe.length = pipe.Length3DCenterToCenter;
      _pipe.displayValue = new List<Mesh> { SolidToSpeckle(pipe.Get3dBody(), out List<string> notes) };
      _pipe.units = ModelUnits;

      // assign additional pipe props
      if (pipe.Name != null) _pipe["name"] = pipe.Name;
      _pipe["description"] = (pipe.Description != null) ? pipe.Description : "";
      _pipe["isPressurePipe"] = true;
      try { _pipe["partType"] = pipe.PartType.ToString(); } catch { }
      try { _pipe["slope"] = pipe.Slope; } catch { }
      try { _pipe["network"] = pipe.NetworkName; } catch { }
      try { _pipe["startOffset"] = pipe.StartOffset; } catch { }
      try { _pipe["endOffset"] = pipe.EndOffset; } catch { }
      try { _pipe["startStation"] = pipe.StartStation; } catch { }
      try { _pipe["endStation"] = pipe.EndStation; } catch { }

      return _pipe;
    }

    // corridors
    // this is composed of assemblies, alignments, and profiles, use point codes to generate featurelines (which will have the 3d curve)
    public Base CorridorToSpeckle(CivilDB.Corridor corridor)
    {
      var _corridor = new Base();

      List<Alignment> alignments = new List<Alignment>();
      List<Profile> profiles = new List<Profile>();
      List<Featureline> featurelines = new List<Featureline>();
      foreach (var baseline in corridor.Baselines)
      {

        // get the collection of featurelines for this baseline
        foreach (var mainFeaturelineCollection in baseline.MainBaselineFeatureLines.FeatureLineCollectionMap) // main featurelines
          foreach (var featureline in mainFeaturelineCollection)
            featurelines.Add(FeaturelineToSpeckle(featureline));
        foreach (var offsetFeaturelineCollection in baseline.OffsetBaselineFeatureLinesCol) // offset featurelines
          foreach (var featurelineCollection in offsetFeaturelineCollection.FeatureLineCollectionMap)
            foreach (var featureline in featurelineCollection)
              featurelines.Add(FeaturelineToSpeckle(featureline));

        // get alignment
        try
        {
          var alignmentId = baseline.AlignmentId;
          var alignment = AlignmentToSpeckle(Trans.GetObject(alignmentId, OpenMode.ForRead) as CivilDB.Alignment);
          if (alignment != null) alignments.Add(alignment);
        }
        catch { }

        // get profile
        try
        {
          var profileId = baseline.ProfileId;
          var profile = ProfileToSpeckle(Trans.GetObject(profileId, OpenMode.ForRead) as CivilDB.Profile);
          if (profile != null) profiles.Add(profile);
        }
        catch { }
      }

      // get corridor surfaces
      List<Mesh> surfaces = new List<Mesh>();
      foreach (var corridorSurface in corridor.CorridorSurfaces)
      {
        try
        {
          var surface = Trans.GetObject(corridorSurface.SurfaceId, OpenMode.ForRead);
          var mesh = ConvertToSpeckle(surface) as Mesh;
          if (mesh != null) surfaces.Add(mesh);
        }
        catch { }
      }

      _corridor["@alignments"] = alignments;
      _corridor["@profiles"] = profiles;
      _corridor["@featurelines"] = featurelines;
      if (corridor.Name != null) _corridor["name"] = corridor.Name;
      if (corridor.Description != null) _corridor["description"] = corridor.Description;
      _corridor["units"] = ModelUnits;
      if (surfaces.Count> 0) _corridor["@surfaces"] = surfaces;

      return _corridor;
    }
  }
}
#endif

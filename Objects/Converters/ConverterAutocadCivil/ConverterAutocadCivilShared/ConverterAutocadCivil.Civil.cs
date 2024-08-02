#if CIVIL
using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using CivilDB = Autodesk.Civil.DatabaseServices;
using Civil = Autodesk.Civil;
using Autodesk.AutoCAD.Geometry;
using Acad = Autodesk.AutoCAD.Geometry;

using Objects.BuiltElements.Civil;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using CivilDataField = Objects.Other.Civil.CivilDataField;
using Polycurve = Objects.Geometry.Polycurve;
using Featureline = Objects.BuiltElements.Featureline;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Mesh = Objects.Geometry.Mesh;
using Pipe = Objects.BuiltElements.Pipe;
using Polyline = Objects.Geometry.Polyline;
using Profile = Objects.BuiltElements.Profile;
using Spiral = Objects.Geometry.Spiral;
using SpiralType = Objects.Geometry.SpiralType;
using Station = Objects.BuiltElements.Station;
using Structure = Objects.BuiltElements.Structure;
using Vector = Objects.Geometry.Vector;
using Speckle.Core.Logging;

namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil
{
  private bool GetCivilDocument(ApplicationObject appObj, out CivilDocument doc)
  {
    doc = CivilApplication.ActiveDocument;
    if (doc is null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not retrieve civil3d document");
    }

    return doc is not null;
  }

  // stations
  public Station StationToSpeckle(CivilDB.Station station)
  {
    var speckleStation = new Station
    {
      location = PointToSpeckle(station.Location),
      type = station.StationType.ToString(),
      number = station.RawStation,
      units = ModelUnits
    };

    return speckleStation;
  }

  // alignments
  public CivilAlignment AlignmentToSpeckle(CivilDB.Alignment alignment)
  {
    // get the profiles
    var profiles = new List<Profile>();
    foreach (ObjectId profileId in alignment.GetProfileIds())
    {
      var profile = Trans.GetObject(profileId, OpenMode.ForRead) as CivilDB.Profile;
      var convertedProfile = ProfileToSpeckle(profile);
      if (convertedProfile != null)
      {
        profiles.Add(convertedProfile);
      }
    }

    // get the station equations
    var equations = new List<double>();
    var directions = new List<bool>();
    foreach (StationEquation stationEquation in alignment.StationEquations)
    {
      equations.AddRange(new List<double> { stationEquation.RawStationBack, stationEquation.StationBack, stationEquation.StationAhead });
      bool equationIncreasing = stationEquation.EquationType.Equals(StationEquationType.Increasing);
      directions.Add(equationIncreasing);
    }

    // get the alignment subentity curves
    List<ICurve> curves = new();
    for (int i = 0; i < alignment.Entities.Count; i++)
    {
      var entity = alignment.Entities.GetEntityByOrder(i);

      var polycurve = new Polycurve(units: ModelUnits, applicationId: entity.EntityId.ToString());
      var segments = new List<ICurve>();
      double length = 0;
      for (int j = 0; j < entity.SubEntityCount; j++)
      {
        AlignmentSubEntity subEntity = entity[j];
        ICurve segment = null;
        switch (subEntity.SubEntityType)
        {
          case AlignmentSubEntityType.Arc:
            var arc = subEntity as AlignmentSubEntityArc;
            segment = AlignmentArcToSpeckle(arc);
            break;
          case AlignmentSubEntityType.Line:
            var line = subEntity as AlignmentSubEntityLine;
            segment = AlignmentLineToSpeckle(line);
            break;
          case AlignmentSubEntityType.Spiral:
            var spiral = subEntity as AlignmentSubEntitySpiral;
            segment = AlignmentSpiralToSpeckle(spiral, alignment);
            break;
          default:
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

    CivilAlignment speckleAlignment = new()
    {
      type = alignment.AlignmentType.ToString(),
      profiles = profiles,
      curves = curves,
      startStation = alignment.StartingStation,
      endStation = alignment.EndingStation,
      stationEquations = equations,
      stationEquationDirections = directions,
      offset = alignment.IsOffsetAlignment ? alignment.OffsetAlignmentInfo.NominalOffset : 0,
      site = alignment.SiteName ?? "",
      style = alignment.StyleName ?? "",
      units = ModelUnits
    };

    AddNameAndDescriptionProperty(alignment.Name, alignment.Description, speckleAlignment);

    // get design speeds
    var designSpeeds = DesignSpeedsToSpeckle(alignment.DesignSpeeds);
    if (designSpeeds.Count > 0)
    {
      speckleAlignment["@designSpeeds"] = designSpeeds;
    }

    // get alignment stations and design speeds
    List<Station> stations = new();
    foreach (CivilDB.Station station in alignment.GetStationSet(StationTypes.All))
    {
      stations.Add(StationToSpeckle(station));
    }
    if (stations.Count > 0)
    {
      speckleAlignment["@stations"] = stations;
    }

    // if offset alignment, also set parent and offset side
    if (alignment.IsOffsetAlignment)
    {
      OffsetAlignmentInfo offsetInfo = alignment.OffsetAlignmentInfo;
      speckleAlignment["offsetSide"] = offsetInfo.Side.ToString();
      if (Trans.GetObject(offsetInfo.ParentAlignmentId, OpenMode.ForRead) is CivilDB.Alignment parent && parent.Name != null)
      {
        speckleAlignment.parent = parent.Name;
      }
    }

    return speckleAlignment;
  }

  private List<Base> DesignSpeedsToSpeckle(DesignSpeedCollection designSpeeds)
  {
    List<Base> speckleDesignSpeeds = new();

    foreach (DesignSpeed designSpeed in designSpeeds)
    {
      Base speckleDesignSpeed = new();
      speckleDesignSpeed["number"] = designSpeed.SpeedNumber;
      speckleDesignSpeed["station"] = designSpeed.Station;
      speckleDesignSpeed["value"] = designSpeed.Value;
      if (!string.IsNullOrEmpty(designSpeed.Comment))
      {
        speckleDesignSpeed["comment"] = designSpeed.Comment;
      }

      speckleDesignSpeeds.Add(speckleDesignSpeed);
    }

    return speckleDesignSpeeds;
  }

  public ApplicationObject AlignmentToNative(Alignment alignment)
  {
    var appObj = new ApplicationObject(alignment.id, alignment.speckle_type) { applicationId = alignment.applicationId };
    var existingObjs = GetExistingElementsByApplicationId(alignment.applicationId);
    var civilAlignment = alignment as CivilAlignment;

    // get civil doc
    if (!GetCivilDocument(appObj, out CivilDocument civilDoc))
    {
      return appObj;
    }

    // create or retrieve alignment, and parent if it exists
    CivilDB.Alignment existingAlignment = existingObjs.Any() ? Trans.GetObject(existingObjs.FirstOrDefault(), OpenMode.ForWrite) as CivilDB.Alignment : null;
    var parent = civilAlignment != null ? GetFromObjectIdCollection(civilAlignment.parent, civilDoc.GetAlignmentIds()) : ObjectId.Null;
    bool isUpdate = true;
    if (existingAlignment == null || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create) // just create a new alignment
    {
      isUpdate = false;

      // get civil props for creation
#region properties
      var name = string.IsNullOrEmpty(alignment.name) ? alignment.applicationId : alignment.name; // names need to be unique on creation (but not send i guess??)
      var layer = Doc.Database.LayerZero;

      // type
      var type = AlignmentType.Centerline;
      if (civilAlignment != null)
      {
        if (Enum.TryParse(civilAlignment.type, out CivilDB.AlignmentType civilType))
        {
          type = civilType;
        }
      }

      // site
      var site = civilAlignment != null ? 
        GetFromObjectIdCollection(civilAlignment.site, civilDoc.GetSiteIds()) : ObjectId.Null;

      // style
      var docStyles = new ObjectIdCollection();
      foreach (ObjectId styleId in civilDoc.Styles.AlignmentStyles)
      {
        docStyles.Add(styleId);
      }

      var style = civilAlignment != null ? 
        GetFromObjectIdCollection(civilAlignment.style, docStyles, true) :  civilDoc.Styles.AlignmentStyles.First();

      // label set style
      var labelStyles = new ObjectIdCollection();
      foreach (ObjectId styleId in civilDoc.Styles.LabelSetStyles.AlignmentLabelSetStyles)
      {
        labelStyles.Add(styleId);
      }

      var label = civilAlignment != null ?
        GetFromObjectIdCollection(civilAlignment["label"] as string, labelStyles, true) : civilDoc.Styles.LabelSetStyles.AlignmentLabelSetStyles.First();
#endregion

      // create the alignment
      var id = ObjectId.Null;
      switch (type)
      {
        case AlignmentType.Offset:
          // create only if parent exists in doc
          if (parent == ObjectId.Null || civilAlignment.offset == 0)
          {
            id = CreateDefaultAlignment(civilDoc, name, site, layer, style, label);
          }

          try
          {
            id = CivilDB.Alignment.CreateOffsetAlignment(name, parent, civilAlignment.offset, style);
          }
          catch (ArgumentException) // throws when name is invalid or offset is too large
          {
            // try to recreate with a unique name
            try
            {
              id = CivilDB.Alignment.CreateOffsetAlignment(CivilDB.Alignment.GetNextUniqueName(name), parent, civilAlignment.offset, style);
            }
            catch (ArgumentException)
            {
              id = CreateDefaultAlignment(civilDoc, name, site, layer, style, label);
            }
          }
          break;
        default:
          id =CreateDefaultAlignment(civilDoc, name, site, layer, style, label);
          break;
      }

      if (!id.IsValid)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Create method returned null");
        return appObj;
      }
      existingAlignment = Trans.GetObject(id, OpenMode.ForWrite) as CivilDB.Alignment;
    }

    if (existingAlignment == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: $"returned null after bake");
      return appObj;
    }

    if (isUpdate)
    {
      appObj.Container = existingAlignment.Layer; // set the appobj container to be the same layer as the existing alignment
    }

    if (parent != ObjectId.Null)
    {
      existingAlignment.OffsetAlignmentInfo.NominalOffset = civilAlignment.offset; // just update the offset
    }
    else
    {
      // create alignment entity curves
      var entities = existingAlignment.Entities;
      if (isUpdate)
      {
        existingAlignment.Entities.Clear(); // remove existing curves
      }

      foreach (var curve in alignment.curves)
      {
        AddAlignmentEntity(curve, ref entities);
      }
    }

    // set start station
    existingAlignment.ReferencePointStation = alignment.startStation;

    // set design speeds if any
    if (civilAlignment["@designSpeeds"] is List<object> speeds)
    {
      foreach (object speed in speeds)
      {
        if (speed is Base speedBase && speedBase["station"] is double station && speedBase["value"] is double value)
        {
          existingAlignment.DesignSpeeds.Add(station, value);
        }
      }
    }

    // update appobj
    var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
    appObj.Update(status: status, createdId: existingAlignment.Handle.ToString(), convertedItem: existingAlignment);
    return appObj;
  }

#region helper methods
  private ObjectId CreateDefaultAlignment(CivilDocument civilDoc, string name, ObjectId site, ObjectId layer, ObjectId style, ObjectId label)
  {
    ObjectId id = ObjectId.Null;
    try // throws when name already exsits or objectIds are invalid
    {
      id = CivilDB.Alignment.Create(civilDoc, name, site, layer, style, label);
    }
    catch (ArgumentException)
    {
      id = CivilDB.Alignment.Create(civilDoc, CivilDB.Alignment.GetNextUniqueName(name), site, layer, style, label);
    }
    return id;
  }

  private SpiralType SpiralTypeToSpeckle(Civil.SpiralType type)
  {
    return type switch
    {
      Civil.SpiralType.Clothoid => SpiralType.Clothoid,
      Civil.SpiralType.Bloss => SpiralType.Bloss,
      Civil.SpiralType.BiQuadratic => SpiralType.Biquadratic,
      Civil.SpiralType.CubicParabola => SpiralType.CubicParabola,
      Civil.SpiralType.Sinusoidal => SpiralType.Sinusoid,
      _ => SpiralType.Unknown,
    };
  }
  private Civil.SpiralType SpiralTypeToNative(SpiralType type)
  {
    return type switch
    {
      SpiralType.Clothoid => Civil.SpiralType.Clothoid,
      SpiralType.Bloss => Civil.SpiralType.Bloss,
      SpiralType.Biquadratic => Civil.SpiralType.BiQuadratic,
      SpiralType.CubicParabola => Civil.SpiralType.CubicParabola,
      SpiralType.Sinusoid => Civil.SpiralType.Sinusoidal,
      _ => Civil.SpiralType.Clothoid,
    };
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
        {
          break;
        }

        var intersectionPoint = PointToNative(intersectionPoints[intersectionPoints.Count / 2]); 
        entities.AddFixedSpiral(entities.LastEntity, start, intersectionPoint , end, SpiralTypeToNative(o.spiralType));
        break;

      case Polycurve o:
        foreach (var segment in o.segments)
        {
          AddAlignmentEntity(segment, ref entities);
        }

        break;

      default:
        break;
    }
  }
  private Line AlignmentLineToSpeckle(CivilDB.AlignmentSubEntityLine line)
  {
    var speckleLine = LineToSpeckle(new LineSegment2d(line.StartPoint, line.EndPoint));
    return speckleLine;
  }
  private Arc AlignmentArcToSpeckle(AlignmentSubEntityArc arc)
  {
    // calculate midpoint of chord as between start and end point
    Point2d chordMid = new((arc.StartPoint.X + arc.EndPoint.X) / 2, (arc.StartPoint.Y + arc.EndPoint.Y) / 2);

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
      if (arc.GreaterThan180) // this can throw : The property gets an invalid value according to the entity's constraint type.
      {
        midPoint = chordMid.Add(unitMidVector.Negate().MultiplyBy(2 * arc.Radius - sagitta));
      }
    }
    catch (InvalidOperationException){ } // continue with original midpoint if GreaterThan180 doesn't apply to this arc

    // create arc
    var speckleArc = ArcToSpeckle(new CircularArc2d(arc.StartPoint, midPoint, arc.EndPoint));
    return speckleArc;
  }  
  private Spiral AlignmentSpiralToSpeckle(AlignmentSubEntitySpiral spiral, CivilDB.Alignment alignment)
  {
    // get plane
    var vX = new Vector3d(Math.Cos(spiral.StartDirection) + spiral.StartPoint.X, Math.Sin(spiral.StartDirection) + spiral.StartPoint.Y, 0);
    var vY = vX.RotateBy(Math.PI / 2, Vector3d.ZAxis);
    var plane = new Acad.Plane(new Point3d(spiral.RadialPoint.X, spiral.RadialPoint.Y, 0), vX, vY);

    // get turns
    int turnDirection = (spiral.Direction == SpiralDirectionType.DirectionLeft) ? 1 : -1;
    double turns = turnDirection * spiral.Delta / (Math.PI * 2);

    // create speckle spiral
    Spiral speckleSpiral = new()
    {
      startPoint = PointToSpeckle(spiral.StartPoint),
      endPoint = PointToSpeckle(spiral.EndPoint),
      length = spiral.Length,
      pitch = 0,
      spiralType = SpiralTypeToSpeckle(spiral.SpiralDefinition),
      plane = PlaneToSpeckle(plane),
      turns = turns
    };

    // create polyline display, default tessellation length is 1
    var tessellation = 1;
    int spiralSegmentCount = Convert.ToInt32(Math.Ceiling(spiral.Length / tessellation));
    spiralSegmentCount = (spiralSegmentCount < 10) ? 10 : spiralSegmentCount;
    double spiralSegmentLength = spiral.Length / spiralSegmentCount;

    List<Point2d> points = new()
    {
      spiral.StartPoint
    };
    for (int i = 1; i < spiralSegmentCount; i++)
    {
      double x = 0;
      double y = 0;
      double z = 0;

      alignment.PointLocation(spiral.StartStation + i * spiralSegmentLength, 0, tolerance, ref x, ref y, ref z);
      points.Add(new Point2d(x, y));
    }

    points.Add(spiral.EndPoint);
    double length = 0;
    for (int j = 1; j < points.Count; j++)
    {
      length += points[j].GetDistanceTo(points[j - 1]);
    }

    Polyline poly = new()
    {
      value = points.SelectMany(o => PointToSpeckle(o).ToList()).ToList(),
      units = ModelUnits,
      closed = spiral.StartPoint == spiral.EndPoint,
      length = length
    };
    speckleSpiral.displayValue = poly;

    return speckleSpiral;
  }

#endregion

  // profiles
  public CivilProfile ProfileToSpeckle(CivilDB.Profile profile)
  {
    // TODO: get surface name of surface profiles from profile view
    CivilProfile speckleProfile = new()
    {
      type = profile.ProfileType.ToString(),
      offset = profile.Offset,
      style = profile.StyleName ?? "",
      startStation = profile.StartingStation,
      endStation = profile.EndingStation,
      units = ModelUnits
    };

    AddNameAndDescriptionProperty(profile.Name, profile.Description, speckleProfile);

    // get the profile entity curves
    List<ICurve> curves = new();
    for (int i = 0; i < profile.Entities.Count; i++)
    {
      ProfileEntity entity = profile.Entities[i];
      switch (entity.EntityType)
      {
        case ProfileEntityType.Circular:
          var circular = ProfileArcToSpeckle(entity as CivilDB.ProfileCircular);
          if (circular != null)
          {
            curves.Add(circular);
          }

          break;
        case ProfileEntityType.Tangent:
          var tangent = ProfileLineToSpeckle(entity as CivilDB.ProfileTangent);
          if (tangent != null)
          {
            curves.Add(tangent);
          }

          break;
        case ProfileEntityType.ParabolaSymmetric:
        case ProfileEntityType.ParabolaAsymmetric:
        default:
          var segment = ProfileGenericToSpeckle(entity.StartStation, entity.StartElevation, entity.EndStation, entity.EndElevation);
          if (segment != null)
          {
            curves.Add(segment);
          }

          break;
      }
    }
    speckleProfile.curves = curves;

    // if offset profile, get offset distance and parent
    speckleProfile.offset = profile.Offset;
    if (profile.ProfileType is ProfileType.OffsetProfile && profile.OffsetParameters.ParentProfileId != ObjectId.Null)
    {
      if (Trans.GetObject(profile.OffsetParameters.ParentProfileId, OpenMode.ForRead) is CivilDB.Profile parent && parent.Name != null)
      {
        speckleProfile.parent = parent.Name;
      }
    }

    // get points of vertical intersection (PVIs)
    List<Point> pvisConverted = new();
    var pvis = new Point3dCollection();
    foreach (ProfilePVI pvi in profile.PVIs)
    {
      double pviStation = 0;
#if CIVIL2024_OR_GREATER
      pviStation = pvi.RawStation;
#else
      pviStation = pvi.Station;
#endif
      pvisConverted.Add(PointToSpeckle(new Point2d(pviStation, pvi.Elevation)));
      pvis.Add(new Point3d(pviStation, pvi.Elevation, 0));
    }
    speckleProfile.pvis = pvisConverted;

    if (pvisConverted.Count > 1)
    {
      speckleProfile.displayValue = PolylineToSpeckle(pvis, profile.Closed);
    }

    return speckleProfile;
  }
  private Line ProfileLineToSpeckle(ProfileTangent tangent)
  {
    var start = new Point2d(tangent.StartStation, tangent.StartElevation);
    var end = new Point2d(tangent.EndStation, tangent.EndElevation);
    return LineToSpeckle(new LineSegment2d(start, end));
  }
  private Arc ProfileArcToSpeckle(ProfileCircular circular)
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
  public Featureline FeaturelineToSpeckle(CivilDB.FeatureLine featureline)
  {
    // get all points
    var points = new List<Point>();
    Point3dCollection allPoints = featureline.GetPoints(Civil.FeatureLinePointType.AllPoints);
    foreach (Point3d point in allPoints)
    {
      points.Add(PointToSpeckle(point));
    }

    // get elevation points
    var ePoints = new List<int>();
    Point3dCollection elevationPoints = featureline.GetPoints(Civil.FeatureLinePointType.ElevationPoint);
    foreach (Point3d ePoint in elevationPoints)
    {
      ePoints.Add(allPoints.IndexOf(ePoint));
    }

    // get pi points
    var piPoints = new List<int>();
    Point3dCollection intersectionPoints = featureline.GetPoints(Civil.FeatureLinePointType.PIPoint);
    foreach (Point3d piPoint in intersectionPoints)
    {
      piPoints.Add(allPoints.IndexOf(piPoint));
    }

    // get displayvalue
    var polyline = PolylineToSpeckle(new Polyline3d(Poly3dType.SimplePoly, intersectionPoints, false));

    // featureline
    Featureline speckleFeatureline = new()
    {
      points = points,
      curve = CurveToSpeckle(featureline.BaseCurve, ModelUnits),
      units = ModelUnits,
      displayValue = new List<Polyline>() { polyline }
    };

    AddNameAndDescriptionProperty(featureline.Name, featureline.Description, speckleFeatureline);
    speckleFeatureline["@piPoints"] = piPoints;
    speckleFeatureline["@elevationPoints"] = ePoints;
    if (featureline.SiteId != null) 
    { 
      speckleFeatureline["site"] = featureline.SiteId.ToString(); 
    }

    return speckleFeatureline;
  }

  private Featureline FeaturelineToSpeckle(CivilDB.CorridorFeatureLine featureline)
  {
    // get all points, the basecurve (no breaks) and the display polylines
    var points = new List<Point>();
    var polylines = new List<Polyline>();

    var polylinePoints = new Point3dCollection();
    var baseCurvePoints = new Point3dCollection();
    for (int i = 0; i < featureline.FeatureLinePoints.Count; i++)
    {
      var point = featureline.FeatureLinePoints[i];
      baseCurvePoints.Add(point.XYZ);
      if (!point.IsBreak) { polylinePoints.Add(point.XYZ); }
      if (polylinePoints.Count > 1 && (i == featureline.FeatureLinePoints.Count - 1 || point.IsBreak ))
      {
        var polyline = PolylineToSpeckle(new Polyline3d(Poly3dType.SimplePoly, polylinePoints, false));
        polylines.Add(polyline);
        polylinePoints.Clear();

      }
      points.Add(PointToSpeckle(point.XYZ));
    }
    var baseCurve = PolylineToSpeckle(new Polyline3d(Poly3dType.SimplePoly, baseCurvePoints, false));

    // create featureline
    var speckleFeatureline = new Featureline
    {
      points = points,
      curve = baseCurve,
      name = featureline.CodeName ?? "",
      displayValue = polylines,
      units = ModelUnits
    };

    return speckleFeatureline;
  }

  // surfaces
  public Mesh SurfaceToSpeckle(TinSurface surface)
  {
    // output vars
    List<double> vertices = new();
    List<int> faces = new ();
    Dictionary<Point3d, int> indices = new();
    
    int indexCounter = 0;
    foreach (var triangle in surface.GetTriangles(false))
    {
      try
      {
        Point3d[] triangleVertices = { triangle.Vertex1.Location, triangle.Vertex2.Location, triangle.Vertex3.Location };
        foreach (Point3d p in triangleVertices)
        {
          if (!indices.ContainsKey(p))
          {
            var scaledP = ToExternalCoordinates(p);
            vertices.Add(scaledP.X);
            vertices.Add(scaledP.Y);
            vertices.Add(scaledP.Z);
            indices.Add(p, indexCounter);
            indexCounter++;
          }
        }
        faces.Add(3);
        faces.Add(indices[triangleVertices[0]]);
        faces.Add(indices[triangleVertices[1]]);
        faces.Add(indices[triangleVertices[2]]);
      }
      finally
      {
        triangle.Dispose();
      }
    }
    
    var mesh = new Mesh(vertices, faces)
    {
      units = ModelUnits,
      bbox = BoxToSpeckle(surface.GeometricExtents)
    };

    // add tin surface props
    AddNameAndDescriptionProperty(surface.Name, surface.Description, mesh);
    Base props = Utilities.GetApplicationProps(surface, typeof(TinSurface), false);
    mesh[CivilPropName] = props;
    
    return mesh;
  }

  public Mesh SurfaceToSpeckle(GridSurface surface)
  {
    // output vars
    var _vertices = new List<Point3d>();
    var faces = new List<int>();

    foreach (var cell in surface.GetCells(false))
    {
      // get vertices
      var faceIndices = new List<int>();
      foreach (var vertex in new List<GridSurfaceVertex>() {cell.BottomLeftVertex, cell.BottomRightVertex, cell.TopLeftVertex, cell.TopRightVertex})
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
    var mesh = new Mesh(vertices, faces)
    {
      units = ModelUnits,
      bbox = BoxToSpeckle(surface.GeometricExtents)
    };

    // add grid surface props
    AddNameAndDescriptionProperty(surface.Name, surface.Description, mesh);

    return mesh;
  }

  public object CivilSurfaceToNative(Mesh mesh)
  {
    if (mesh[CivilPropName] is not Base props)
    {
      return null;
    }

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
    if (!GetCivilDocument(appObj, out CivilDocument civilDoc))
    {
      return appObj;
    }

    // create or retrieve tin surface
    CivilDB.TinSurface surface = existingObjs.Any() ? Trans.GetObject(existingObjs.FirstOrDefault(), OpenMode.ForWrite) as CivilDB.TinSurface : null;
    bool isUpdate = true;
    if (surface == null || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create) // just create a new surface
    {
      isUpdate = false;

      // get civil props for creation
      var name = string.IsNullOrEmpty(mesh["name"] as string) ? 
        string.IsNullOrEmpty(mesh.applicationId) ? 
        mesh.id : 
        mesh.applicationId :
        mesh["name"] as string;
      ObjectId layer = Doc.Database.LayerZero;
      ObjectIdCollection docStyles = new();
      ObjectId style = ObjectId.Null;
      foreach (ObjectId styleId in civilDoc.Styles.SurfaceStyles)
      {
        docStyles.Add(styleId);
      }
      
      if (docStyles.Count != 0 )
      {
        style = GetFromObjectIdCollection(props["style"] as string, docStyles, true);
      }

      // add new surface to doc
      var id = ObjectId.Null;
      id = style == ObjectId.Null ? TinSurface.Create(Doc.Database, name) : TinSurface.Create(name, style);
      if (!id.IsValid)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Create method returned null");
        return appObj;
      }

      surface = Trans.GetObject(id, OpenMode.ForWrite) as TinSurface;
    }

    if (surface == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: $"retrieved or baked surface was null");
      return appObj;
    }

    if (isUpdate)
    {
      appObj.Container = surface.Layer; // set the appobj container to be the same layer as the existing alignment
      surface.DeleteVertices(surface.Vertices); // remove existing vertices
    }

    // add all vertices
    var vertices = new Point3dCollection();
    var meshVertices = mesh.GetPoints().Select(o => PointToNative(o)).ToList();
    meshVertices.ForEach(o => vertices.Add(o));
    surface.AddVertices(vertices);
    
    // loop through faces to create an edge dictionary by vertex, which includes all other vertices this vertex is connected to
    int i = 0;
    var edges = new Dictionary<Point3d, List<Point3d>>();
    foreach (var vertex in meshVertices)
    {
      edges.Add(vertex, new List<Point3d>());
    }

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
      var vertex = surface.FindVertexAtXY(edgeStart.X, edgeStart.Y);
      var correctEdges = new List<Point3d>();
      foreach (CivilDB.TinSurfaceEdge currentEdge in vertex.Edges)
      {
        if (edges[edgeStart].Contains(currentEdge.Vertex2.Location))
        {
          correctEdges.Add(currentEdge.Vertex2.Location);
        }

        currentEdge.Dispose();
      }
      vertex.Dispose();

      foreach (var vertexToAdd in edges[edgeStart]) 
      {
        if (correctEdges.Contains(vertexToAdd))
        {
          continue;
        }
        
        var a1 = surface.FindVertexAtXY(edgeStart.X, edgeStart.Y);
        var a2 = surface.FindVertexAtXY(vertexToAdd.X, vertexToAdd.Y);
        surface.AddLine(a1, a2);
        a1.Dispose();
        a2.Dispose();
      }
    }
    
    // loop through and delete any edges
    var edgesToDelete = new List<TinSurfaceEdge>();
    foreach(TinSurfaceVertex vertex in surface.Vertices)
    {
      if (vertex.Edges.Count > edges[vertex.Location].Count)
      {
        foreach (TinSurfaceEdge modifiedEdge in vertex.Edges)
        {
          if (!edges[vertex.Location].Contains(modifiedEdge.Vertex2.Location) && !edges[modifiedEdge.Vertex2.Location].Contains(vertex.Location))
          {
            edgesToDelete.Add(modifiedEdge);
          }
        }
      }

      vertex.Dispose();
    }
    if (edgesToDelete.Count > 0)
    {
      surface.DeleteLines(edgesToDelete);
      surface.Rebuild();
    }
    
    // update appobj
    var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
    appObj.Update(status: status, createdId: surface.Handle.ToString(), convertedItem: surface);
    return appObj;
  }

  // structures
  public Structure StructureToSpeckle(CivilDB.Structure structure)
  {
    // get ids pipes that are connected to this structure
    var pipeIds = new List<string>();
    for (int i = 0; i < structure.ConnectedPipesCount; i++)
    {
      pipeIds.Add(structure.get_ConnectedPipe(i).ToString());
    }

    Structure speckleStructure = new()
    {
      location = PointToSpeckle(structure.Location, ModelUnits),
      pipeIds = pipeIds,
      displayValue = new List<Mesh>() { SolidToSpeckle(structure.Solid3dBody, out List<string>_) },
      units = ModelUnits
    };

    // assign additional structure props
    AddNameAndDescriptionProperty(structure.Name, structure.Description, speckleStructure);
    speckleStructure["partData"] = PartDataRecordToSpeckle(structure.PartData);

    try { speckleStructure["station"] = structure.Station; } catch (Exception ex) when (!ex.IsFatal()) { }
    try { speckleStructure["network"] = structure.NetworkName; } catch (Exception ex) when (!ex.IsFatal()) { }
    try { speckleStructure["rotation"] = structure.Rotation; } catch (Exception e) when (!e.IsFatal()) { }
    try { speckleStructure["sumpDepth"] = structure.SumpDepth; } catch (Exception e) when (!e.IsFatal()) { }
    try { speckleStructure["rimElevation"] = structure.RimElevation; } catch (Exception e) when (!e.IsFatal()) { }
    try { speckleStructure["sumpElevation"] = structure.SumpElevation; } catch (Exception e) when (!e.IsFatal()) { }
    try { speckleStructure["lengthOuter"] = structure.Length; } catch (Exception e) when (!e.IsFatal()) { }
    try { speckleStructure["lengthInner"] = structure.InnerLength; } catch (Exception e) when (!e.IsFatal()) { }
    try { speckleStructure["structureId"] = structure.Id.ToString(); } catch (Exception ex) when (!ex.IsFatal()) { }

    return speckleStructure;
  }

  // part data
  /// <summary>
  /// Converts PartData into a list of DataField
  /// </summary>
  private Base PartDataRecordToSpeckle(PartDataRecord partData)
  {
    Base partDataBase = new();
    List<CivilDataField> fields = new();

    foreach (PartDataField partField in partData.GetAllDataFields())
    {
      CivilDataField field = new(partField.Name, partField.DataType.ToString(), partField.Value, partField.Units.ToString(),partField.Context.ToString(), null);
      partDataBase[partField.Name] = field;
    }

    return partDataBase;
  }

#if CIVIL2022_OR_GREATER
  /// <summary>
  /// Converts PressureNetworkPartData into a list of DataField
  /// </summary>
  private List<CivilDataField> PartDataRecordToSpeckle(PressureNetworkPartData partData)
  {
    List<CivilDataField> fields = new();

    foreach (PressurePartProperty partField in partData)
    {
      CivilDataField field = new(partField.Name, partField.GetType().ToString(), partField.Value, null, null, partField.DisplayName);
      fields.Add(field);
    }

    return fields;
  }
#endif

  // pipes
  // TODO: add pressure fittings
  public Pipe PipeToSpeckle(CivilDB.Pipe pipe)
  {
    // get the pipe curve
    // rant: if this is a straight or curved pipe, the BaseCurve prop is fake news && will return a DB.line with start and endpoints set to [0,0,0] & [0,0,1]
    // do not use CurveToSpeckle(basecurve) 😡
    ICurve curve;
    switch (pipe.SubEntityType)
    {
      case PipeSubEntityType.Straight:
        var line = new Acad.LineSegment3d(pipe.StartPoint, pipe.EndPoint);
        curve = LineToSpeckle(line);
        break;
      case PipeSubEntityType.Curved:
        curve = ArcToSpeckle(pipe.Curve2d);
        break;
      default:
        curve = CurveToSpeckle(pipe.BaseCurve); // basecurve is fake news, but we're still sending the other types with props for now
        break;
    }

    Pipe specklePipe = new()
    {
      baseCurve = curve,
      diameter = pipe.InnerDiameterOrWidth,
      length = pipe.Length3DToInsideEdge,
      displayValue = new List<Mesh> { SolidToSpeckle(pipe.Solid3dBody, out List<string> notes) },
      units = ModelUnits
    };

    // assign additional pipe props
    AddNameAndDescriptionProperty(pipe.Name, pipe.Description, specklePipe);
    specklePipe["partData"] = PartDataRecordToSpeckle(pipe.PartData);

    try { specklePipe["shape"] = pipe.CrossSectionalShape.ToString(); } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["slope"] = pipe.Slope; } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["flowDirection"] = pipe.FlowDirection.ToString(); } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["flowRate"] = pipe.FlowRate; } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["network"] = pipe.NetworkName; } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["startOffset"] = pipe.StartOffset; } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["endOffset"] = pipe.EndOffset; } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["startStation"] = pipe.StartStation; } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["endStation"] = pipe.EndStation; } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["startStructureId"] = pipe.StartStructureId.ToString(); } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["endStructureId"] = pipe.EndStructureId.ToString(); } catch(Exception ex) when(!ex.IsFatal()) { }
    try { specklePipe["pipeId"] = pipe.Id.ToString(); } catch (Exception ex) when (!ex.IsFatal()) { }

    return specklePipe;
  }
  
  public Pipe PipeToSpeckle(PressurePipe pipe)
  {
    // get the pipe curve
    // rant: if this is a straight or curved pipe, the BaseCurve prop is fake news && will return a DB.line with start and endpoints set to [0,0,0] & [0,0,1]
    // do not use CurveToSpeckle(basecurve) 😡
    ICurve curve;
    switch (pipe.BaseCurve)
    {
      case Autodesk.AutoCAD.DatabaseServices.Line:
        var line = new LineSegment3d(pipe.StartPoint, pipe.EndPoint);
        curve = LineToSpeckle(line);
        break;
#if CIVIL2024_OR_GREATER
      case Autodesk.AutoCAD.DatabaseServices.Arc:
        var arc = pipe.CurveGeometry.GetArc2d();
        curve = ArcToSpeckle(arc);
        break;
#endif
      default:
        curve = CurveToSpeckle(pipe.BaseCurve);
        break;
    }

    Pipe specklePipe = new()
    {
      baseCurve = curve,
      diameter = pipe.InnerDiameter,
      length = pipe.Length3DCenterToCenter,
      displayValue = new List<Mesh> { SolidToSpeckle(pipe.Get3dBody(), out List<string> notes) },
      units = ModelUnits
    };

    // assign additional pipe props
    AddNameAndDescriptionProperty(pipe.Name, pipe.Description, specklePipe);
#if CIVIL2022_OR_GREATER
    specklePipe["partData"] = PartDataRecordToSpeckle(pipe.PartData);
#endif

    specklePipe["isPressurePipe"] = true;
    try { specklePipe["partType"] = pipe.PartType.ToString(); } catch (Exception e) when (!e.IsFatal()) { }
    try { specklePipe["slope"] = pipe.Slope; } catch (Exception e) when (!e.IsFatal()) { }
    try { specklePipe["network"] = pipe.NetworkName; } catch (Exception e) when (!e.IsFatal()) { }
    try { specklePipe["startOffset"] = pipe.StartOffset; } catch (Exception e) when (!e.IsFatal()) { }
    try { specklePipe["endOffset"] = pipe.EndOffset; } catch (Exception e) when (!e.IsFatal()) { }
    try { specklePipe["startStation"] = pipe.StartStation; } catch (Exception e) when (!e.IsFatal()) { }
    try { specklePipe["endStation"] = pipe.EndStation; } catch (Exception e) when (!e.IsFatal()) { }
    try { specklePipe["pipeId"] = pipe.Id.ToString(); } catch (Exception ex) when (!ex.IsFatal()) { }

    return specklePipe;
  }

  // corridors
  // this is composed of assemblies, alignments, and profiles, use point codes to generate featurelines (which will have the 3d curve)

  private CivilDataField AppliedSubassemblyParamToSpeckle(IAppliedSubassemblyParam param)
  {
    CivilDataField baseParam = new(param.KeyName, param.ValueType.Name, param.ValueAsObject, null, null, param.DisplayName);

    return baseParam;
  }

  private CivilAppliedSubassembly AppliedSubassemblyToSpeckle(AppliedSubassembly appliedSubassembly)
  {
    // retrieve subassembly name
    Subassembly subassembly = Trans.GetObject(appliedSubassembly.SubassemblyId, OpenMode.ForRead) as Subassembly;

    // get the calculated shapes
    List<CivilCalculatedShape> speckleShapes = new();
    foreach (CalculatedShape shape in appliedSubassembly.Shapes)
    {
      CivilCalculatedShape speckleShape = CalculatedShapeToSpeckle(shape);
      speckleShapes.Add(speckleShape);
    }

    Point soePoint = PointToSpeckle(appliedSubassembly.OriginStationOffsetElevationToBaseline);
    List<CivilDataField> speckleParameters = appliedSubassembly.Parameters.Select(p => AppliedSubassemblyParamToSpeckle(p)).ToList();

    CivilAppliedSubassembly speckleAppliedSubassembly = new(appliedSubassembly.SubassemblyId.ToString(), subassembly.Name, speckleShapes, soePoint, speckleParameters)
    {
      units = ModelUnits
    };

    return speckleAppliedSubassembly;
  }

  private CivilAppliedAssembly AppliedAssemblyToSpeckle(AppliedAssembly appliedAssembly)
  {
    // get the applied subassemblies
    List<CivilAppliedSubassembly> speckleSubassemblies = new();
    foreach (AppliedSubassembly appliedSubassembly in appliedAssembly.GetAppliedSubassemblies())
    {
      CivilAppliedSubassembly speckleSubassembly = AppliedSubassemblyToSpeckle(appliedSubassembly);
      speckleSubassemblies.Add(speckleSubassembly);
    }

    double? adjustedElevation = null;
    try
    {
      adjustedElevation = appliedAssembly.AdjustedElevation;
    }
    catch (ArgumentException e) when (!e.IsFatal())
    {
      // Do nothing. Leave the value as null.
    }

    CivilAppliedAssembly speckleAppliedAssembly = new(speckleSubassemblies, adjustedElevation, ModelUnits);

    return speckleAppliedAssembly;
  }

  private CivilBaselineRegion BaselineRegionToSpeckle(BaselineRegion region)
  {
    // get the region assembly
    Assembly assembly = Trans.GetObject(region.AssemblyId, OpenMode.ForRead) as Assembly;

    // get the applied assemblies by station
    List<CivilAppliedAssembly> speckleAppliedAssemblies = new();
    double[] sortedStations = region.SortedStations();
    for (int i = 0; i < sortedStations.Length; i++)
    {
      double station = sortedStations[i];
      CivilAppliedAssembly speckleAssembly = AppliedAssemblyToSpeckle(region.AppliedAssemblies[i]);
      speckleAssembly["station"] = station;
      speckleAppliedAssemblies.Add(speckleAssembly);
    }

    // create the speckle region
    CivilBaselineRegion speckleRegion = new(region.Name, region.StartStation, region.EndStation, assembly.Id.ToString(), assembly.Name, speckleAppliedAssemblies)
    {
      units = ModelUnits
    };

    return speckleRegion;
  }

  private CivilCalculatedShape CalculatedShapeToSpeckle(CalculatedShape shape)
  {
    List<string> codes = shape.CorridorCodes.ToList();
    List<CivilCalculatedLink> speckleLinks = new();
    foreach (CalculatedLink link in shape.CalculatedLinks)
    {
      CivilCalculatedLink speckleLink = CalculatedLinkToSpeckle(link);
      speckleLinks.Add(speckleLink);
    }

    CivilCalculatedShape speckleCalculatedShape = new(codes, speckleLinks, shape.Area, ModelUnits);
    return speckleCalculatedShape;
  }

  private CivilCalculatedLink CalculatedLinkToSpeckle(CalculatedLink link)
  {
    List<string> codes = link.CorridorCodes.ToList();
    List<CivilCalculatedPoint> specklePoints = new();
    foreach (CalculatedPoint point in link.CalculatedPoints)
    {
      CivilCalculatedPoint specklePoint = CalculatedPointToSpeckle(point);
      specklePoints.Add(specklePoint);
    }

    CivilCalculatedLink speckleLink = new(codes, specklePoints)
    {
      units = ModelUnits
    };

    return speckleLink;
  }

  private CivilCalculatedPoint CalculatedPointToSpeckle(CalculatedPoint point)
  {
    Point specklePoint = PointToSpeckle(point.XYZ);
    List<string> codes = point.CorridorCodes.ToList();
    Vector normalBaseline = VectorToSpeckle(point.NormalToBaseline);
    Vector normalSubAssembly = VectorToSpeckle(point.NormalToSubassembly);
    Point soePoint = PointToSpeckle(point.StationOffsetElevationToBaseline);
    CivilCalculatedPoint speckleCalculatedPoint = new(specklePoint, codes, normalBaseline, normalSubAssembly, soePoint)
    {
      units = ModelUnits
    };

    return speckleCalculatedPoint;
  }

  private CivilBaseline BaselineToSpeckle(CivilDB.Baseline baseline)
  {
    CivilBaseline speckleBaseline = null;

    // get the speckle regions
    List<CivilBaselineRegion> speckleRegions = new();
    foreach (BaselineRegion region in baseline.BaselineRegions)
    {
      CivilBaselineRegion speckleRegion = BaselineRegionToSpeckle(region);
      speckleRegions.Add(speckleRegion);
    }

    // get profile and alignment if nonfeaturelinebased
    // for featureline based corridors, accessing AlignmentId and ProfileId will return NULL
    // and throw an exception ""This operation on feature line based baseline is invalid".
    if (!baseline.IsFeatureLineBased())
    {
      // get the speckle alignment
      var alignment = Trans.GetObject(baseline.AlignmentId, OpenMode.ForRead) as CivilDB.Alignment;
      CivilAlignment speckleAlignment = AlignmentToSpeckle(alignment);

      // get the speckle profile
      var profile = Trans.GetObject(baseline.ProfileId, OpenMode.ForRead) as CivilDB.Profile;
      CivilProfile speckleProfile = ProfileToSpeckle(profile);

      speckleBaseline = new(baseline.Name, speckleRegions, baseline.SortedStations().ToList(), baseline.StartStation, baseline.EndStation, speckleAlignment, speckleProfile)
      {
        units = ModelUnits
      };
    }
    else
    {
      // get the baseline featureline
      var featureline = Trans.GetObject(baseline.FeatureLineId, OpenMode.ForRead) as CivilDB.FeatureLine;
      Featureline speckleFeatureline = FeaturelineToSpeckle(featureline);

      speckleBaseline = new(baseline.Name, speckleRegions, baseline.SortedStations().ToList(), baseline.StartStation, baseline.EndStation, speckleFeatureline)
      {
        units = ModelUnits
      };
    }
    
    return speckleBaseline;
  }

  public Base CorridorToSpeckle(Corridor corridor)
  {
    List<Featureline> featurelines = new();
    List<CivilBaseline> baselines = new();
    foreach (CivilDB.Baseline baseline in corridor.Baselines)
    {
      CivilBaseline speckleBaseline = BaselineToSpeckle(baseline);
      baselines.Add(speckleBaseline);  

      // get the collection of featurelines for this baseline
      foreach (FeatureLineCollection mainFeaturelineCollection in baseline.MainBaselineFeatureLines.FeatureLineCollectionMap) // main featurelines
      {
        foreach (CorridorFeatureLine featureline in mainFeaturelineCollection)
        {
          featurelines.Add(FeaturelineToSpeckle(featureline));
        }
      }

      foreach (BaselineFeatureLines offsetFeaturelineCollection in baseline.OffsetBaselineFeatureLinesCol) // offset featurelines
      {
        foreach (FeatureLineCollection featurelineCollection in offsetFeaturelineCollection.FeatureLineCollectionMap)
        {
          foreach (CorridorFeatureLine featureline in featurelineCollection)
          {
            featurelines.Add(FeaturelineToSpeckle(featureline));
          }
        }
      }
    }

    // get corridor surfaces
    List<Mesh> surfaces = new();
    foreach (CorridorSurface corridorSurface in corridor.CorridorSurfaces)
    {
      try
      {
        var surface = Trans.GetObject(corridorSurface.SurfaceId, OpenMode.ForRead);
        if (ConvertToSpeckle(surface) is Mesh mesh)
        {
          surfaces.Add(mesh);
        }
      }
      catch (Exception e) when (!e.IsFatal())
      {
        SpeckleLog.Logger.Warning(e, $"Could not convert and add surface to corridor");
      }
    }

    var corridorBase = new Base();
    corridorBase["@featurelines"] = featurelines;
    corridorBase["@baselines"] = baselines;
    AddNameAndDescriptionProperty(corridor.Name, corridor.Description, corridorBase);
    corridorBase["units"] = ModelUnits;
    if (surfaces.Count > 0)
    {
      corridorBase["@surfaces"] = surfaces;
    }

    return corridorBase;
  }
}
#endif

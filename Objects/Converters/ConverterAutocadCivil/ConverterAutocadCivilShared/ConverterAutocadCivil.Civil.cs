#if CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024
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
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Objects.BuiltElements.Civil;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
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
  public Station StationToSpeckle(CivilDB.Station station, double? designSpeed)
  {
    var speckleStation = new Station
    {
      location = PointToSpeckle(station.Location),
      type = station.StationType.ToString(),
      number = station.RawStation,
      units = ModelUnits
    };
    
    if (designSpeed is not null)
    {
      speckleStation["designSpeed"] = designSpeed.Value;
    }

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
      style = alignment.StyleName ?? ""
    };

    AddNameAndDescriptionProperty(alignment.Name, alignment.Description, speckleAlignment);

    // get alignment stations and design speeds
    List<Station> stations = new();
    Dictionary<double,double> designSpeeds =new();
    foreach (DesignSpeed designSpeed in alignment.DesignSpeeds)
    {
      designSpeeds.Add(designSpeed.Station, designSpeed.Value);
    }
    foreach (CivilDB.Station station in alignment.GetStationSet(StationTypes.All))
    {
      double? speed = designSpeeds.ContainsKey(station.RawStation) ? designSpeeds[station.RawStation] : null;
      stations.Add(StationToSpeckle(station, speed));
    }
    if (stations.Any())
    {
      speckleAlignment["@stations"] = stations;
    }

    // if offset alignment, also set parent and offset side
    if (alignment.IsOffsetAlignment)
    {
      speckleAlignment["offsetSide"] = alignment.OffsetAlignmentInfo.Side.ToString();
      try
      {
        if (Trans.GetObject(alignment.OffsetAlignmentInfo.ParentAlignmentId, OpenMode.ForRead) is CivilDB.Alignment parent && parent.Name != null)
        {
          speckleAlignment.parent = parent.Name;
        }
      }
      catch { }
    }

    return speckleAlignment;
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

      try
      {
        // add new alignment to doc
        // ⚠ this will throw if name is not unique!!
        var id = ObjectId.Null;
        switch (type)
        {
          case AlignmentType.Offset:
            // create only if parent exists in doc
            if (parent == ObjectId.Null)
            {
              goto default;
            }

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
        existingAlignment = Trans.GetObject(id, OpenMode.ForWrite) as CivilDB.Alignment;
      }
      catch (Exception e)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
        return appObj;
      }
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
    if (civilAlignment["@stations"] is List<object> stations)
    {
      foreach (Station station in stations.Cast<Station>())
      {
        if (station["designSpeed"] is double designSpeed)
        {
          existingAlignment.DesignSpeeds.Add(station.number, designSpeed);
        }
      }
    }

    // update appobj
    var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
    appObj.Update(status: status, createdId: existingAlignment.Handle.ToString(), convertedItem: existingAlignment);
    return appObj;
  }

#region helper methods
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
  private Arc AlignmentArcToSpeckle(CivilDB.AlignmentSubEntityArc arc)
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
      if (arc.GreaterThan180) // sometimes this prop throws an exception??
      {
        midPoint = chordMid.Add(unitMidVector.Negate().MultiplyBy(2 * arc.Radius - sagitta));
      }
    }
    catch { }

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
      endStation = profile.EndingStation
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
    try
    {
      if (profile.OffsetParameters.ParentProfileId != ObjectId.Null)
      {
        if (Trans.GetObject(profile.OffsetParameters.ParentProfileId, OpenMode.ForRead) is CivilDB.Profile parent && parent.Name != null)
        {
          speckleProfile.parent = parent.Name;
        }
      }
    }
    catch { }

    // get points of vertical intersection (PVIs)
    List<Point> pvisConverted = new();
    var pvis = new Point3dCollection();
    foreach (ProfilePVI pvi in profile.PVIs)
    {
      pvisConverted.Add(PointToSpeckle(new Point2d(pvi.Station, pvi.Elevation)));
      pvis.Add(new Point3d(pvi.Station, pvi.Elevation, 0));
    }
    speckleProfile.pvis = pvisConverted;


    if (pvisConverted.Count > 1)
    {
      speckleProfile.displayValue = PolylineToSpeckle(pvis, profile.Closed);
    }

    speckleProfile.units = ModelUnits;

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

  /*
  public ApplicationObject ProfileToNative(Profile profile)
  {
    var appObj = new ApplicationObject(profile.id, profile.speckle_type) { applicationId = profile.applicationId };
    var existingObjs = GetExistingElementsByApplicationId(profile.applicationId);
    var civilProfile = profile as CivilProfile;

    // get civil doc
    if (!GetCivilDocument(out CivilDocument civilDoc))
      return appObj;

    // create or retrieve alignment, and parent if it exists
    CivilDB.Profile _profile = existingObjs.Any() ? Trans.GetObject(existingObjs.FirstOrDefault(), OpenMode.ForWrite) as CivilDB.Profile : null;
    var parent = civilProfile != null ? GetFromObjectIdCollection(civilProfile.parent, civilDoc.get()) : ObjectId.Null;
    bool isUpdate = true;
    if (_profile == null || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create) // just create a new profile
    {
      isUpdate = false;

      // get civil props for creation
#region properties
      var name = string.IsNullOrEmpty(profile.name) ? profile.applicationId : profile.name; // names need to be unique on creation (but not send i guess??)
      var layer = Doc.Database.LayerZero;

      // type
      var type = CivilDB.ProfileType.File;
      if (civilProfile != null)
        if (Enum.TryParse(civilProfile.type, out CivilDB.ProfileType civilType))
          type = civilType;

      // style
      var docStyles = new ObjectIdCollection();
      foreach (ObjectId styleId in civilDoc.Styles.ProfileStyles) docStyles.Add(styleId);
      var style = civilProfile != null ?
        GetFromObjectIdCollection(civilProfile.style, docStyles, true) : civilDoc.Styles.ProfileStyles.First();

#endregion

      try
      {
        // add new profile to doc
        // ⚠ this will throw if name is not unique!!
        var id = ObjectId.Null;
        switch (type)
        {
          // A surface profile—often called an existing ground (EG) profile—is extracted from a surface, showing the changes in elevation along a particular route
          case CivilDB.ProfileType.EG:
           
            if (parent == ObjectId.Null) goto default; 
            try
            {
              id = CivilDB.Profile.CreateFromSurface(Doc, );
            }
            catch
            {
            }
            break;
          default:
            try
            {
              id = CivilDB.Profile.CreateFromGeCurve();
            }
            catch
            {
              
            }
            break;
        }
        if (!id.IsValid)
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Create method returned null");
          return appObj;
        }
        _profile = Trans.GetObject(id, OpenMode.ForWrite) as CivilDB.Profile;
      }
      catch (System.Exception e)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
        return appObj;
      }
    }

    if (_profile == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: $"returned null after bake");
      return appObj;
    }

  }
  */

  // featurelines
  public Featureline FeatureLineToSpeckle(CivilDB.FeatureLine featureline)
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

    /*
    // get bulges at pi point indices
    int count = (featureline.Closed) ? featureline.PointsCount : featureline.PointsCount - 1;
    List<double> bulges = new List<double>();
    for (int i = 0; i < count; i++) bulges.Add(featureline.GetBulge(i));
    var piBulges = new List<double>();
    foreach (var index in indices) piBulges.Add(bulges[index]);
    */

    // get displayvalue
    var polyline = PolylineToSpeckle(new Polyline3d(Poly3dType.SimplePoly, intersectionPoints, false));

    // featureline
    Featureline speckleFeatureline = new()
    {
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
      if (polylinePoints.Count > 0 && (i == featureline.FeatureLinePoints.Count - 1 || point.IsBreak ))
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
    List<Point3d> vertices = new();
    var faces = new List<int>();
    foreach (var triangle in surface.GetTriangles(false))
    {
      var triangleVertices = new List<Point3d> { triangle.Vertex1.Location, triangle.Vertex2.Location, triangle.Vertex3.Location };

#if CIVIL2023 || CIVIL2024 // skip any triangles that are hidden in the surface!
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
        if (!vertices.Contains(vertex))
        {
          faceIndices.Add(vertices.Count);
          vertices.Add(vertex);
        }
        else
        {
          faceIndices.Add(vertices.IndexOf(vertex));
        }
      }

      // get face
      faces.AddRange(new List<int> { 3, faceIndices[0], faceIndices[1], faceIndices[2] });

      triangle.Dispose();
    }

    var speckleVertices = vertices.SelectMany(o => PointToSpeckle(o).ToList()).ToList();

    var mesh = new Mesh(speckleVertices, faces)
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
      var name = string.IsNullOrEmpty(mesh["name"] as string) ? mesh.applicationId : mesh["name"] as string;
      var layer = Doc.Database.LayerZero;
      var docStyles = new ObjectIdCollection();
      foreach (ObjectId styleId in civilDoc.Styles.SurfaceStyles)
      {
        docStyles.Add(styleId);
      }

      var style = props["style"] as string != null ?
        GetFromObjectIdCollection(props["style"] as string, docStyles) : civilDoc.Styles.SurfaceStyles.First();

      // add new surface to doc
      // ⚠ this will throw if name is empty?
      var id = ObjectId.Null;
      try
      {
        id = CivilDB.TinSurface.Create(name, style);
      }
      catch (Exception e)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
        return appObj;
      }
      if (!id.IsValid)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Create method returned null");
        return appObj;
      }
      surface = Trans.GetObject(id, OpenMode.ForWrite) as CivilDB.TinSurface;
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
      displayValue = new List<Mesh>() { SolidToSpeckle(structure.Solid3dBody, out List<string> notes) },
      units = ModelUnits
    };

    // assign additional structure props
    AddNameAndDescriptionProperty(structure.Name, structure.Description, speckleStructure);
    try{ speckleStructure["grate"] = structure.Grate; } catch{ }
    try{ speckleStructure["station"] = structure.Station; } catch{ }
    try{ speckleStructure["network"] = structure.NetworkName; } catch{ }

    return speckleStructure;
  }

  // pipes
  // TODO: add pressure fittings
  public Pipe PipeToSpeckle(CivilDB.Pipe pipe)
  {
    // get the pipe curve
    ICurve curve;
    switch (pipe.SubEntityType)
    {
      case PipeSubEntityType.Straight:
        var line = new Acad.LineSegment3d(pipe.StartPoint, pipe.EndPoint);
        curve = LineToSpeckle(line);
        break;
      default:
        curve = CurveToSpeckle(pipe.BaseCurve);
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

    try { specklePipe["shape"] = pipe.CrossSectionalShape.ToString(); } catch { }
    try { specklePipe["slope"] = pipe.Slope; } catch { }
    try { specklePipe["flowDirection"] = pipe.FlowDirection.ToString(); } catch { }
    try { specklePipe["flowRate"] = pipe.FlowRate; } catch { }
    try { specklePipe["network"] = pipe.NetworkName; } catch { }
    try { specklePipe["startOffset"] = pipe.StartOffset; } catch { }
    try { specklePipe["endOffset"] = pipe.EndOffset; } catch { }
    try { specklePipe["startStation"] = pipe.StartStation; } catch { }
    try { specklePipe["endStation"] = pipe.EndStation; } catch { }
    try { specklePipe["startStructure"] = pipe.StartStructureId.ToString(); } catch { }
    try { specklePipe["endStructure"] = pipe.EndStructureId.ToString(); } catch { }

    return specklePipe;
  }
  public Pipe PipeToSpeckle(PressurePipe pipe)
  {
    // get the pipe curve
    ICurve curve;
    switch (pipe.BaseCurve)
    {
      case AcadDB.Line:
        var line = new LineSegment3d(pipe.StartPoint, pipe.EndPoint);
        curve = LineToSpeckle(line);
        break;
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
    specklePipe["isPressurePipe"] = true;
    try { specklePipe["partType"] = pipe.PartType.ToString(); } catch { }
    try { specklePipe["slope"] = pipe.Slope; } catch { }
    try { specklePipe["network"] = pipe.NetworkName; } catch { }
    try { specklePipe["startOffset"] = pipe.StartOffset; } catch { }
    try { specklePipe["endOffset"] = pipe.EndOffset; } catch { }
    try { specklePipe["startStation"] = pipe.StartStation; } catch { }
    try { specklePipe["endStation"] = pipe.EndStation; } catch { }

    return specklePipe;
  }

  // corridors
  // this is composed of assemblies, alignments, and profiles, use point codes to generate featurelines (which will have the 3d curve)
  public Base CorridorToSpeckle(CivilDB.Corridor corridor)
  {
    List<Alignment> alignments = new();
    List<Profile> profiles = new();
    List<Featureline> featurelines = new();
    foreach (var baseline in corridor.Baselines)
    {

      // get the collection of featurelines for this baseline
      foreach (var mainFeaturelineCollection in baseline.MainBaselineFeatureLines.FeatureLineCollectionMap) // main featurelines
      {
        foreach (var featureline in mainFeaturelineCollection)
        {
          featurelines.Add(FeaturelineToSpeckle(featureline));
        }
      }

      foreach (var offsetFeaturelineCollection in baseline.OffsetBaselineFeatureLinesCol) // offset featurelines
      {
        foreach (var featurelineCollection in offsetFeaturelineCollection.FeatureLineCollectionMap)
        {
          foreach (var featureline in featurelineCollection)
          {
            featurelines.Add(FeaturelineToSpeckle(featureline));
          }
        }
      }

      // get alignment
      try
      {
        var alignmentId = baseline.AlignmentId;
        var alignment = AlignmentToSpeckle(Trans.GetObject(alignmentId, OpenMode.ForRead) as CivilDB.Alignment);
        if (alignment != null)
        {
          alignments.Add(alignment);
        }
      }
      catch { }

      // get profile
      try
      {
        var profileId = baseline.ProfileId;
        var profile = ProfileToSpeckle(Trans.GetObject(profileId, OpenMode.ForRead) as CivilDB.Profile);
        if (profile != null)
        {
          profiles.Add(profile);
        }
      }
      catch { }
    }

    // get corridor surfaces
    List<Mesh> surfaces = new();
    foreach (var corridorSurface in corridor.CorridorSurfaces)
    {
      try
      {
        var surface = Trans.GetObject(corridorSurface.SurfaceId, OpenMode.ForRead);
        if (ConvertToSpeckle(surface) is Mesh mesh)
        {
          surfaces.Add(mesh);
        }
      }
      catch (Exception e) 
      { }
    }

    var corridorBase = new Base();
    corridorBase["@alignments"] = alignments;
    corridorBase["@profiles"] = profiles;
    corridorBase["@featurelines"] = featurelines;
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

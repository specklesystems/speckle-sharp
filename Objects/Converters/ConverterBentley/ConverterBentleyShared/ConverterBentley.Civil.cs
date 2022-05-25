#if (OPENROADS || OPENRAIL || OPENBRIDGE)
using Objects.Geometry;
using Objects.Primitive;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Alignment = Objects.BuiltElements.Alignment;
using Station = Objects.BuiltElements.Station;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using BMIU = Bentley.MstnPlatformNET.InteropServices.Utilities;
using BIM = Bentley.Interop.MicroStationDGN;
using CifGM = Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.GeometryModel.SDK.Edit;
using Bentley.CifNET.SDK.Edit;
using Bentley.CifNET.GeometryModel;
using Bentley.CifNET.SDK;
using LinGeom = Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.Formatting;

using Bentley.DgnPlatformNET.DgnEC;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;

namespace Objects.Converter.Bentley
{
  public partial class ConverterBentley
  {
    // stations
    public Station StationToSpeckle(CifGM.StationEquation station)
    {
      return null;
    }

    public Base FeatureLineToSpeckle(CifGM.FeaturizedModelEntity entity)
    {
      return null;
    }

    

    public List<ICurve> CurveVectorToSpeckle(CurveVector curve, string units = null)
    {
      var segments = new List<ICurve>();
      
      if (curve != null)
      {
        foreach (var primitive in curve)
        {
          var curvePrimitiveType = primitive.GetCurvePrimitiveType();

          switch (curvePrimitiveType)
          {
            case CurvePrimitive.CurvePrimitiveType.Line:
              primitive.TryGetLine(out DSegment3d segment);
              segments.Add(LineToSpeckle(segment, units));
              break;
            case CurvePrimitive.CurvePrimitiveType.Arc:
              primitive.TryGetArc(out DEllipse3d arc);
              segments.Add(ArcToSpeckle(arc, units));
              break;
            case CurvePrimitive.CurvePrimitiveType.LineString:
              var pointList = new List<DPoint3d>();
              primitive.TryGetLineString(pointList);
              segments.Add(PolylineToSpeckle(pointList));
              break;
            case CurvePrimitive.CurvePrimitiveType.BsplineCurve:
              var spline = primitive.GetBsplineCurve();
              segments.Add(BSplineCurveToSpeckle(spline, units));
              break;
            case CurvePrimitive.CurvePrimitiveType.Spiral:
              var spiralSpline = primitive.GetProxyBsplineCurve();
              segments.Add(SpiralCurveElementToCurve(spiralSpline));
              break;
          }
        }
      }

      return segments;
    }

    

    // alignments
    public Alignment AlignmentToSpeckle(CifGM.Alignment alignment)
    {
      var model = GeomModel;
      var ent3d = model.LinearEntities3d;
      foreach(var ent in ent3d)
      {
        if (ent.Alignment != null)
        {
          var el = ent.Element;
        }
      }

      var al = model.Alignments;

      var _alignment = new Alignment();

      CifGM.StationFormatSettings settings = CifGM.StationFormatSettings.GetStationFormatSettingsForModel(Model);
      var stationFormatter = new CifGM.StationingFormatter(alignment);

      var curve2d = CurveToSpeckle(alignment.Element as DisplayableElement, ModelUnits);
      _alignment.curves = new List<ICurve> { curve2d };
      _alignment["@curves2d"] = curve2d;

      //var curve = alignment.Geometry;     
      //var speckleCurves = CurveVectorToSpeckle(curve, ModelUnits);
      //_alignment.curves = speckleCurves;

      //_alignment.displayValue = speckleCurves;

      var linearGeometry = alignment.LinearGeometry;
      double EndPosition = linearGeometry.Length;

      List<ICurve> segments = new List<ICurve> { };
      if (linearGeometry is LinGeom.LinearComplex)
      {
        LinGeom.LinearComplex lc = (linearGeometry as LinGeom.LinearComplex);
        foreach (LinGeom.LinearElement le in lc.GetSubLinearElements())
        {
          var cv = le.GetCurveVector();
          segments.AddRange(CurveVectorToSpeckle(cv, ModelUnits));
        }
      }
      else if (linearGeometry is LinGeom.LineString)
      {
        LinGeom.LineString ls = (linearGeometry as LinGeom.LineString);
        var cv = ls.GetCurveVector();
        segments.AddRange(CurveVectorToSpeckle(cv, ModelUnits));
      }
      else
      {
        var cv = linearGeometry.GetCurveVector();
        segments.AddRange(CurveVectorToSpeckle(cv, ModelUnits));
      }

      _alignment["@linearGeometry"] = segments;

      Dictionary<string, object> properties = new Dictionary<string, object>();
      var instance = alignment.DgnECInstance;
      foreach (IECPropertyValue propertyValue in instance)
      {
        if (propertyValue != null)
        {
          properties = GetValue(properties, propertyValue);
        }
      }
      var instanceName = instance.ClassDefinition.Name;

      Base bentleyProperties = new Base();
      foreach (string propertyName in properties.Keys)
      {
        Object value = properties[propertyName];

        if (value.GetType().Name == "DPoint3d")
        {
          bentleyProperties[propertyName] = ConvertToSpeckle(value);
        }
        else
        {
          bentleyProperties[propertyName] = value;
        }
      }

      _alignment["@properties"] = bentleyProperties;

      if (alignment.Name != null)
        _alignment.name = alignment.Name;

      if (alignment.FeatureName != null)
        _alignment["featureName"] = alignment.FeatureName;

      if (alignment.FeatureDefinition != null)
        _alignment["featureDefinitionName"] = alignment.FeatureDefinition.Name;

      var stationing = alignment.Stationing;
      if (stationing != null)
      {
        _alignment.startStation = stationing.StartStation;
        _alignment.endStation = alignment.LinearGeometry.Length;  // swap for end station

        var region = stationing.GetStationRegionFromDistanceAlong(stationing.StartStation);

        // handle station equations
        var equations = new List<double>();
        var formattedEquation = new List<string>();
        //var directions = new List<bool>();
        foreach (var stationEquation in stationing.StationEquations)
        {
          string stnVal = "";
          stationFormatter.FormatStation(ref stnVal, stationEquation.DistanceAlong, settings);
          formattedEquation.Add(stnVal);

          // DistanceAlong represents Back Station/BackLocation, EquivalentStation represents Ahead Station
          equations.AddRange(new List<double> { stationEquation.DistanceAlong, stationEquation.DistanceAlong, stationEquation.EquivalentStation });

        }
        _alignment.stationEquations = equations;
        _alignment["formattedStationEquations"] = formattedEquation;
        //_alignment.stationEquationDirections = directions;
      }
      else
      {
        _alignment.startStation = 0;
      }

      if(alignment.Profiles != null && alignment.Profiles.Count() > 0)
      {
        var _profiles = new List<Base> { };
        foreach (var profile in alignment.Profiles)
        {
          _profiles.Add(ProfileToSpeckle(profile));
        }
        _alignment["@profiles"] = _profiles;
      }

      if(alignment.ActiveLinearEntity3d != null)
      {
        var el3d = alignment.ActiveLinearEntity3d.Element;
        var curve3d = ConvertToSpeckle(el3d);
        _alignment["@curves3d"] = curve3d;
        _alignment.curves.Add((ICurve)curve3d);
      }

      _alignment.units = ModelUnits;

      return _alignment;
    }

    public CifGM.Alignment AlignmentToNative(Alignment alignment)
    {
      var baseCurve = alignment.baseCurve;
      var nativeCurve = CurveToNative(baseCurve);

      ConsensusConnectionEdit con = ConsensusConnectionEdit.GetActive();
      con.StartTransientMode();
      AlignmentEdit alignmentEdit = (CifGM.Alignment.CreateFromElement(con, nativeCurve)) as AlignmentEdit;
      if (alignmentEdit.DomainObject == null)
        return null;

      alignmentEdit.AddStationing(0, alignment.startStation, true);

      if (alignment.stationEquations != null)
      {
        var formatted = (List<string>)alignment["formattedStationEquations"];
        for (int i = 0; i < formatted.Count(); i++)
        {
          var locationAlong = alignment.stationEquations[(i * 3) + 1];
          var ahead = formatted[i];
          alignmentEdit.AddStationEquation(ahead, locationAlong);
        }
      }

      con.PersistTransients();

      return null;
    }

    public Base GetProfileGeometry(LinGeom.ProfileElement ele)
    {
      Base _profileGeom = new Base();
      if (ele is LinGeom.ProfileLine)
      {
        var line = (LinGeom.ProfileLine)ele;
        var cv = ele.GetCurveVector();
        _profileGeom["linearGeometry"] = CurveVectorToSpeckle(cv);

        _profileGeom["tangentGrade"] = line.Slope;
        _profileGeom["tangentLength"] = line.ProjectedLength;
      }
      else if (ele is LinGeom.ProfileParabola)
      {
        var parabola = (LinGeom.ProfileParabola)ele;
        var cv = ele.GetCurveVector();
        _profileGeom["linearGeometry"] = CurveVectorToSpeckle(cv);

        _profileGeom["PVC"] = Point3dToSpeckle(parabola.StartPoint.Coordinates);
        _profileGeom["PVI"] = Point3dToSpeckle(parabola.VPIPoint);
        _profileGeom["PVT"] = Point3dToSpeckle(parabola.EndPoint.Coordinates);

        double projectedLeftLength = parabola.VPIPoint.X - ele.StartPoint.Coordinates.X;
        double projectedRightLength = parabola.EndPoint.Coordinates.X - parabola.VPIPoint.X;

        LinGeom.LinearPoint summit = parabola.SummitPoint;
        if (summit != null &&
            Math.Sign(parabola.StartSlope) != Math.Sign(parabola.EndSlope) &&
            projectedLeftLength != 0.0 &&
            projectedRightLength != 0.0 &&
            (parabola.StartSlope - parabola.EndSlope) != 0.0 &&
            (parabola.EndSlope - parabola.StartSlope) != 0.0)
        {
          if (parabola.EndSlope - parabola.StartSlope > 0.0)
            _profileGeom["VLOW"] = Point3dToSpeckle(summit.Coordinates);
          else
            _profileGeom["VHIGH"] = Point3dToSpeckle(summit.Coordinates);
        }

        _profileGeom["length"] = parabola.ProjectedLength;
        _profileGeom["entranceGrade"] = parabola.StartSlope;
        _profileGeom["exitGrade"] = parabola.EndSlope;
      }

      _profileGeom["profileType"] = ele.GetType().Name;

      return _profileGeom;
    }

    // profiles
    public Base ProfileToSpeckle(CifGM.Profile profile)
    {
      var _profile = new Base();
      _profile["name"] = profile.Name;
      _profile["length"] = profile.ProfileGeometry.ProjectedLength;

      var _profileGeom = new List<Base>();
      if (profile.ProfileGeometry is LinGeom.ProfileComplex)
      {
        LinGeom.ProfileComplex complex = profile.ProfileGeometry as LinGeom.ProfileComplex;
        foreach (LinGeom.ProfileElement ele in complex.GetSubProfileElements())
          _profileGeom.Add(GetProfileGeometry(ele));
      }
      else
        _profileGeom.Add(GetProfileGeometry(profile.ProfileGeometry));

      _profile["profileGeometry"] = _profileGeom;

      return _profile;
    }

    // corridors
    public Base CorridorToSpeckle(CifGM.Corridor corridor)
    {
      var element = corridor.Element;
      var _corridor = ConvertToSpeckle(element) as Base;

      var alignment = corridor.CorridorAlignment;
      if (alignment != null)
      {
        var convertedAlignment = AlignmentToSpeckle(alignment);
        _corridor["alignment"] = convertedAlignment;
      }

      var stations = corridor.KeyStations;
      var profile = corridor.CorridorProfile;
      var surfaces = corridor.CorridorSurfaces;

      if (corridor.Name != null)
        _corridor["name"] = corridor.Name;

      _corridor["units"] = ModelUnits;

      return _corridor;
    }
  }
}
#endif
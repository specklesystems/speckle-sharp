#if (OPENROADS || OPENRAIL)
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
using Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.Formatting;
using System.Collections;

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

    // alignments
    public Alignment AlignmentToSpeckle(CifGM.Alignment alignment)
    {
      var _alignment = new Alignment();

      CifGM.StationFormatSettings settings = CifGM.StationFormatSettings.GetStationFormatSettingsForModel(Model);
      var stationFormatter = new CifGM.StationingFormatter(alignment);

      _alignment.baseCurve = CurveToSpeckle(alignment.Element as DisplayableElement, ModelUnits);  

      _alignment.profiles = alignment.Profiles
        .Select(x => ProfileToSpeckle(x))
        .Cast<BuiltElements.Profile>()
        .ToList();

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

    // profiles
    public Base ProfileToSpeckle(CifGM.Profile profile)
    {
      var outProfile = new Objects.BuiltElements.Profile();

      var geo = profile.ProfileGeometry;

      var profileWithFallback = new List<(ProfileElement profileGeo, DisplayableElement displayGeo)>();

      if (geo is IEnumerable complexProfile)
      {
        profileWithFallback = complexProfile
          .Cast<object>()
          .Where(x => x is ProfileElement)
          .Cast<ProfileElement>()
          .ToList();
      }
      else
      {
        profileWithFallback = new List<ProfileElement>() { geo };
      }

      outProfile.curves = profileWithFallback.ConvertAll(x => ProfileCurveToSpeckle(x));

      outProfile.name= profile.Name;
      outProfile.startStation = profile.ProfileGeometry.StartPoint.DistanceAlong; ///????

      return outProfile;

    }

    private ICurve ProfileCurveToSpeckle(ProfileElement geo)
    {


      var profiles = alignment.Profiles
    .Select(x => x.Element as DisplayableElement)
    .Select(x => CurveToSpeckle(x, ModelUnits))
    .ToList();




      if (geo is ProfileCircularArc circularArc)
      {
        return CurveToSpeckle(circularArc.Sta;
      }
      else if (geo is ProfileParabola parabola)
      {
        return null;
      }
      else if (geo is ProfileLineString lineString)
      {
        return null;
      }
      else if (geo is ProfileLine line)
      {
        return null;
      }
      else return null;
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

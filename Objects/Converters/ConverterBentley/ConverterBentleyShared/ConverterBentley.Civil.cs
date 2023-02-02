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
using Objects.Structural.Analysis;

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

      _alignment.curves = CurveToSpeckleCurveList(alignment.Element as DisplayableElement, ModelUnits);

      _alignment.profiles = new List<BuiltElements.Profile> { };

      //To match LandXML export behaviour we only export the Active profile
      if (alignment.ActiveProfile is CifGM.Profile p)
      {
        var activeProfile = ProfileToSpeckle(p, ModelUnits) as BuiltElements.Profile;

        _alignment.profiles.Add(activeProfile);
      }

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
        _alignment.endStation = alignment.LinearGeometry.Length + stationing.EndStation;  // swap for end station

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
      if (alignment?.curves?.Any() is null) return null;

      ICurve singleBaseCurve;

      if (alignment.curves?.Count == 1)
      {
        singleBaseCurve = alignment.curves.Single();
      }
      else
      {
        //Not 100% clear on how best to handle the conversion between multiple curves and single element
        singleBaseCurve = new Polycurve()
        {
          segments = alignment.curves
        };
      }

      var nativeCurve = CurveToNative(singleBaseCurve);

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
    public Base ProfileToSpeckle(CifGM.Profile profile, string modelUnits = "m")
    {
      var curves = new List<ICurve>();      

      switch (profile.ProfileGeometry)
      {
        case ProfileParabola profileParabola:

          /// This has thrown exceptions in the past, but should be resolved now that only
          /// the active profile is being exported, as it was throwing on isolated curve
          /// elements with no assigned feature properties.
          /// 
          /// Pulled out as its own case so it's a bit clearer why the failure is happening
          /// if it reoccurs.

          try
          {
            curves.AddRange(CurveToSpeckleCurveList(profile.Element as DisplayableElement), modelUnits);
            break;
          }
          catch (Exception ex)
          {
            throw new Exception("Failed to import isolated profile Parabola", ex);
          }

        default:
          curves.AddRange(CurveToSpeckleCurveList(profile.Element as DisplayableElement, modelUnits));
          break;
      }

      var outProfile = new BuiltElements.Profile
      {
        curves = curves,

        name = profile.Name,
        
      };

      // The assumption here is that profiles exist in chainage space (x == chainage, y == elevation) 
      // so the associated bounding box will be in the same space.
      if (curves.Any() 
        && curves.First() is IHasBoundingBox startBox 
        && curves.Last() is IHasBoundingBox endBox
        && startBox.bbox?.xSize?.start is double startChainage
        && endBox.bbox?.xSize?.end is double endChainage)
      {
        outProfile.startStation = startChainage;
        outProfile.endStation = endChainage;
      }

      if (profile.FeatureName is string featureName)
        outProfile[nameof(featureName)] = featureName;

      if (profile.FeatureDefinition?.Name is string featureDefinitionName)
        outProfile[nameof(featureDefinitionName)] = featureDefinitionName;

      return outProfile;

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

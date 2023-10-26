using CSiAPIv1;
using Objects.Structural.Loading;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void LoadPatternToNative(LoadGravity gravityLoad)
    {
      var selfWeight = -1 * gravityLoad.gravityFactors.z;
      var loadType = GetAndConvertToCSIPatternType(gravityLoad.loadCase.loadType);
      Model.LoadPatterns.Add(gravityLoad.name, loadType, selfWeight);
    }

    public LoadCase LoadPatternToSpeckle(string loadPatternName)
    {
      LoadCase speckleLoadCase =
        new()
        {
          loadType = GetAndConvertCSILoadType(loadPatternName),
          name = loadPatternName,
          // load pattern name cannot be duplicated in etabs, so safe for applicationId use
          applicationId = loadPatternName
        };

      var selfWeight = GetSelfWeightMultiplier(loadPatternName);

      //Encoding loadPatterns selfweight multiplier within
      Geometry.Vector gravityVector = selfWeight != 0 ? new(0, 0, -selfWeight) : new(0, 0, 0);

      LoadGravity gravityLoad =
        new(speckleLoadCase, gravityVector)
        {
          name = loadPatternName,
          applicationId = $"{loadPatternName}:self-weight"
        };

      SpeckleModel.loads.Add(gravityLoad);

      if (!SpeckleModel.loads.Contains(speckleLoadCase))
      {
        SpeckleModel.loads.Add(speckleLoadCase);
      }

      return speckleLoadCase;
    }

    public LoadCase LoadCaseToSpeckle(string name)
    {
      LoadCase speckleLoadCase = new();
      return speckleLoadCase;
    }

    public LoadCase LoadPatternCaseToSpeckle(string loadPatternName)
    {
      //Converts just the load case name
      LoadCase speckleLoadCase =
        new()
        {
          loadType = GetAndConvertCSILoadType(loadPatternName),
          name = loadPatternName,
          // load pattern name cannot be duplicated in etabs, so safe for applicationId use
          applicationId = loadPatternName
        };

      if (SpeckleModel.loads.Contains(speckleLoadCase))
      {
        SpeckleModel.loads.Add(speckleLoadCase);
      }

      return speckleLoadCase;
    }

    public eLoadPatternType GetAndConvertToCSIPatternType(LoadType loadType)
    {
      return loadType switch
      {
        LoadType.Dead => eLoadPatternType.Dead,
        LoadType.SuperDead => eLoadPatternType.SuperDead,
        LoadType.Live => eLoadPatternType.Live,
        LoadType.ReducibleLive => eLoadPatternType.ReduceLive,
        LoadType.SeismicStatic => eLoadPatternType.Quake,
        LoadType.Snow => eLoadPatternType.Snow,
        LoadType.Wind => eLoadPatternType.Wind,
        LoadType.Other => eLoadPatternType.Other,
        _ => eLoadPatternType.Other
      };
    }

    public LoadType GetAndConvertCSILoadType(string name)
    {
      eLoadPatternType patternType = new();

      Model.LoadPatterns.GetLoadType(name, ref patternType);

      return patternType switch
      {
        eLoadPatternType.Dead => LoadType.Dead,
        eLoadPatternType.SuperDead => LoadType.SuperDead,
        eLoadPatternType.Live => LoadType.Live,
        eLoadPatternType.ReduceLive => LoadType.ReducibleLive,
        eLoadPatternType.Quake => LoadType.SeismicStatic,
        eLoadPatternType.Wind => LoadType.Wind,
        eLoadPatternType.Snow => LoadType.Snow,
        eLoadPatternType.Other => LoadType.Other,
        _ => LoadType.Other
      };
    }

    public double GetSelfWeightMultiplier(string name)
    {
      double selfWeightMultiplier = 0;

      Model.LoadPatterns.GetSelfWTMultiplier(name, ref selfWeightMultiplier);

      return selfWeightMultiplier;
    }
  }
}

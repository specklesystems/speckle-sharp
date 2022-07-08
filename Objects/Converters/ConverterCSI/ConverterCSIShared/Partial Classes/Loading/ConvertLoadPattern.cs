using CSiAPIv1;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void LoadPatternToNative(LoadGravity gravityLoad)
    {
      var selfweight = -1 * gravityLoad.gravityFactors.z;
      var LoadType = GetAndConvertToCSIPatternType(gravityLoad.loadCase.loadType);
      Model.LoadPatterns.Add(gravityLoad.name, LoadType, selfweight);
    }
    public LoadCase LoadPatternToSpeckle(string loadPatternName)
    {
      var speckleLoadCase = new LoadCase();
      speckleLoadCase.loadType = GetAndConvertCSILoadType(loadPatternName);
      speckleLoadCase.name = loadPatternName;

      // load pattern name cannot be duplicated in etabs, so safe for applicationId use
      speckleLoadCase.applicationId = loadPatternName;

      var selfweight = GetSelfWeightMultiplier(loadPatternName);

      //Encoding loadPatterns selfweight multiplier within 
      if (selfweight != 0)
      {
        var gravityVector = new Geometry.Vector(0, 0, -selfweight);
        var gravityLoad = new LoadGravity(speckleLoadCase, gravityVector);
        gravityLoad.name = loadPatternName;

        gravityLoad.applicationId = $"{loadPatternName}:self-weight";

        SpeckleModel.loads.Add(gravityLoad);
      }
      else
      {
        var gravityVector = new Geometry.Vector(0, 0, 0);
        var gravityLoad = new LoadGravity(speckleLoadCase, gravityVector);
        gravityLoad.name = loadPatternName;
        gravityLoad.applicationId = $"{loadPatternName}:self-weight";
        SpeckleModel.loads.Add(gravityLoad);
      }
      if (SpeckleModel.loads.Contains(speckleLoadCase)) { }
      else { SpeckleModel.loads.Add(speckleLoadCase); }


      return speckleLoadCase;
    }

    public LoadCase LoadCaseToSpeckle(string name)
    {
      var speckleLoadCase = new LoadCase();
      return speckleLoadCase;
    }

    public LoadCase LoadPatternCaseToSpeckle(string loadPatternName)
    {
      //Converts just the load case name
      var speckleLoadCase = new LoadCase();
      speckleLoadCase.loadType = GetAndConvertCSILoadType(loadPatternName);
      speckleLoadCase.name = loadPatternName;

      // load pattern name cannot be duplicated in etabs, so safe for applicationId use
      speckleLoadCase.applicationId = loadPatternName;

      if (!SpeckleModel.loads.Contains(speckleLoadCase)) { }
      else { SpeckleModel.loads.Add(speckleLoadCase); }
      return speckleLoadCase;
    }

    public eLoadPatternType GetAndConvertToCSIPatternType(LoadType loadType)
    {
      switch (loadType)
      {
        case LoadType.Dead:
          return eLoadPatternType.Dead;
        case LoadType.SuperDead:
          return eLoadPatternType.SuperDead;
        case LoadType.Live:
          return eLoadPatternType.Live;
        case LoadType.ReducibleLive:
          return eLoadPatternType.ReduceLive;
        case LoadType.SeismicStatic:
          return eLoadPatternType.Quake;
        case LoadType.Snow:
          return eLoadPatternType.Snow;
        case LoadType.Wind:
          return eLoadPatternType.Wind;
        case LoadType.Other:
          return eLoadPatternType.Other;
        default:
          return eLoadPatternType.Other;
      }
    }
    public LoadType GetAndConvertCSILoadType(string name)
    {
      eLoadPatternType patternType = new eLoadPatternType();

      Model.LoadPatterns.GetLoadType(name, ref patternType);

      switch (patternType)
      {
        case eLoadPatternType.Dead:
          return LoadType.Dead;

        case eLoadPatternType.SuperDead:
          return LoadType.SuperDead;

        case eLoadPatternType.Live:
          return LoadType.Live;

        case eLoadPatternType.ReduceLive:
          return LoadType.ReducibleLive;

        case eLoadPatternType.Quake:
          return LoadType.SeismicStatic;

        case eLoadPatternType.Wind:
          return LoadType.Wind;

        case eLoadPatternType.Snow:
          return LoadType.Snow;

        case eLoadPatternType.Other:
          return LoadType.Other;

        default:
          return LoadType.Other; // Other (less frequent) load types to be converted later.
      }
    }

    public double GetSelfWeightMultiplier(string name)
    {
      double selfWeightMultiplier = 0;

      Model.LoadPatterns.GetSelfWTMultiplier(name, ref selfWeightMultiplier);

      return selfWeightMultiplier;
    }
  }
}
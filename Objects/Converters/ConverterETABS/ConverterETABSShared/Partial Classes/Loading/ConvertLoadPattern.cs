using ETABSv1;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public object LoadPatternToNative(Load gravityLoad)
        {
            return gravityLoad.name;
            
        }
        public LoadCase LoadPatternToSpeckle(string loadPatternName)
        {
            var speckleLoadCase = new LoadCase();


            speckleLoadCase.loadType = GetAndConvertEtabsLoadType(loadPatternName);

            speckleLoadCase.name = loadPatternName;
            var selfweight = GetSelfWeightMultiplier(loadPatternName);
           
            //Encoding loadPatterns selfweight multiplier within 
            if (selfweight != 0)
            {
                var gravityVector = new Geometry.Vector(0, 0, -selfweight);
                var gravityLoad = new LoadGravity(speckleLoadCase,gravityVector);
                SpeckleModel.loads.Add(gravityLoad);
            }
            else
            {
                var gravityVector = new Geometry.Vector(0, 0, 0);
                var gravityLoad = new LoadGravity(speckleLoadCase, gravityVector);
                SpeckleModel.loads.Add(gravityLoad);
            }
                
            return speckleLoadCase;
        }

        public LoadType GetAndConvertEtabsLoadType(string name)
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

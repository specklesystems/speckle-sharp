using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Results;
using Objects.Structural.Results.ApplicationSpecific.CSi;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConverterCSIShared.Models
{
  internal class Element1DAnalyticalResultConverter
  {
    private readonly Model speckleModel;
    private readonly cSapModel sapModel;
    private readonly HashSet<string> frameNames;
    private readonly HashSet<string> pierNames;
    private readonly HashSet<string> spandrelNames;
    private readonly Dictionary<string, Base> loadCombinationsAndCases;
    public Element1DAnalyticalResultConverter(
      Model speckleModel,
      cSapModel sapModel,
      HashSet<string> frameNames,
      HashSet<string> pierNames,
      HashSet<string> spandrelNames, 
      IEnumerable<LoadCombination> loadCombinations,
      IEnumerable<LoadCase> loadCases
    )
    {
      this.speckleModel = speckleModel;
      this.sapModel = sapModel;
      this.frameNames = frameNames;
      this.pierNames = pierNames;
      this.spandrelNames = spandrelNames;
      this.sapModel = sapModel;

      this.loadCombinationsAndCases = new();
      foreach (var combo in loadCombinations)
      {
        this.loadCombinationsAndCases.Add(combo.name, combo);
      }
      foreach (var loadCase in loadCases)
      {
        this.loadCombinationsAndCases.Add(loadCase.name, loadCase);
      }
    }

    public void AnalyticalResultsToSpeckle()
    {
      foreach (Base element in speckleModel.elements)
      {
        if (element is not CSIElement1D element1D)
        {
          continue;
        }

        AnalyticalResults results = new()
        {
          resultsByLoadCombination = GetAnalysisResultsForElement1D(element1D).Cast<Result>().ToList()
        };
        element1D.AnalysisResults = results;
      }
    }

    private ICollection<LoadCombinationResult1D> GetAnalysisResultsForElement1D(Element1D element1D)
    {
      Dictionary<string, LoadCombinationResult1D> loadCombinationResults = new();
      if (frameNames.Contains(element1D.name))
      {
        return GetAnalysisResultsForFrame(element1D);
      }
      else if (pierNames.Contains(element1D.name))
      {
        return GetAnalysisResultsForPier(element1D);
      }
      else if (spandrelNames.Contains(element1D.name))
      {
        return GetAnalysisResultsForSpandrel(element1D);
      }
      throw new SpeckleException($"Unable to find category for Element1D with name {element1D.name} and CSi ID {element1D.applicationId}");
    }

    private ICollection<LoadCombinationResult1D> GetAnalysisResultsForFrame(Element1D element1D)
    {
      // Reference variables for CSI API
      int numberOfResults = 0;
      string[] obj,
        elm,
        loadCase,
        stepType;
      obj = elm = loadCase = stepType = Array.Empty<string>();
      double[] objSta,
        elmSta,
        stepNum,
        p,
        v2,
        v3,
        t,
        m2,
        m3;
      objSta = elmSta = stepNum = p = v2 = v3 = t = m2 = m3 = Array.Empty<double>();

      sapModel.Results.FrameForce(
        element1D.name,
        eItemTypeElm.ObjectElm,
        ref numberOfResults,
        ref obj,
        ref objSta,
        ref elm,
        ref elmSta,
        ref loadCase,
        ref stepType,
        ref stepNum,
        ref p,
        ref v2,
        ref v3,
        ref t,
        ref m2,
        ref m3
      );

      // Value used to normalized output station of forces between 0 and 1
      var lengthOf1dElement = objSta.Max();

      Dictionary<string, LoadCombinationResult1D> loadCombinationResults = new();
      for (int i = 0; i < numberOfResults; i++)
      {
        CSiResult1D result = new()
        {
          positionAlongBeam = (float)(objSta[i] / lengthOf1dElement),
          axialForce = (float)p[i],
          shearForceStrongAxis = (float)v2[i],
          shearForceWeakAxis = (float)v3[i],
          torsionForce = (float)t[i],
          momentAboutStrongAxis = (float)m3[i],
          momentAboutWeakAxis = (float)m2[i]
        };
        GetOrCreateResult(loadCombinationResults, loadCase[i]).results1D.Add(result);
      }
      return loadCombinationResults.Values;
    }
    private LoadCombinationResult1D GetOrCreateResult(Dictionary<string, LoadCombinationResult1D> dict, string loadCaseName)
    {
      if (!dict.TryGetValue(loadCaseName, out LoadCombinationResult1D comboResults))
      {
        Base loadCaseOrCombination = loadCombinationsAndCases[loadCaseName];
        comboResults = new LoadCombinationResult1D(loadCaseOrCombination, new());
        dict[loadCaseName] = comboResults;
      }
      return comboResults;
    }
    private ICollection<LoadCombinationResult1D> GetAnalysisResultsForPier(Element1D element1D)
    {
      // Reference variables for CSI API
      int numberOfResults = 0;
      string[] storyName,
        pierName,
        loadCase,
        location;
      storyName = pierName = loadCase = location = new string[1];
      double[] p,
        v2,
        v3,
        t,
        m2,
        m3;
      p = v2 = v3 = t = m2 = m3 = new double[1];

      sapModel.Results.PierForce(
        ref numberOfResults,
        ref storyName,
        ref pierName,
        ref loadCase,
        ref location,
        ref p,
        ref v2,
        ref v3,
        ref t,
        ref m2,
        ref m3
      );

      Dictionary<string, LoadCombinationResult1D> loadCombinationResults = new();
      for (int i = 0; i < numberOfResults; i++)
      {
        if (pierName[i] != element1D.name)
        {
          continue;
        }
        CSiResult1D result = new()
        {
          positionAlongBeam = 0,
          axialForce = (float)p[i],
          shearForceStrongAxis = (float)v2[i],
          shearForceWeakAxis = (float)v3[i],
          torsionForce = (float)t[i],
          momentAboutStrongAxis = (float)m3[i],
          momentAboutWeakAxis = (float)m2[i]
        };
        GetOrCreateResult(loadCombinationResults, loadCase[i]).results1D.Add(result);
      }
      return loadCombinationResults.Values;
    }
    private ICollection<LoadCombinationResult1D> GetAnalysisResultsForSpandrel(Element1D element1D)
    {
      // Reference variables for CSI API
      int numberOfResults = 0;
      string[] storyName,
        spandrelName,
        loadCase,
        location;
      storyName = spandrelName = loadCase = location = new string[1];
      double[] p,
        v2,
        v3,
        t,
        m2,
        m3;
      p = v2 = v3 = t = m2 = m3 = new double[1];

      sapModel.Results.SpandrelForce(
        ref numberOfResults,
        ref storyName,
        ref spandrelName,
        ref loadCase,
        ref location,
        ref p,
        ref v2,
        ref v3,
        ref t,
        ref m2,
        ref m3
      );

      Dictionary<string, LoadCombinationResult1D> loadCombinationResults = new();
      for (int i = 0; i < numberOfResults; i++)
      {
        if (spandrelName[i] != element1D.name)
        {
          continue;
        }
        CSiResult1D result = new()
        {
          positionAlongBeam = 0,
          axialForce = (float)p[i],
          shearForceStrongAxis = (float)v2[i],
          shearForceWeakAxis = (float)v3[i],
          torsionForce = (float)t[i],
          momentAboutStrongAxis = (float)m3[i],
          momentAboutWeakAxis = (float)m2[i]
        };
        GetOrCreateResult(loadCombinationResults, loadCase[i]).results1D.Add(result);
      }
      return loadCombinationResults.Values;
    }
  }
}

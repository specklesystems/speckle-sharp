using System;
using System.Collections.Generic;
using System.Linq;
using ConverterCSIShared.Extensions;
using CSiAPIv1;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Results;
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
    private readonly bool sendBeamForces;
    private readonly bool sendBraceForces;
    private readonly bool sendColumnForces;
    private readonly bool sendOtherForces;
    public Element1DAnalyticalResultConverter(
      Model speckleModel,
      cSapModel sapModel,
      HashSet<string> frameNames,
      HashSet<string> pierNames,
      HashSet<string> spandrelNames,
      IEnumerable<LoadCombination> loadCombinations,
      IEnumerable<LoadCase> loadCases,
      bool sendBeamForces,
      bool sendBraceForces,
      bool sendColumnForces,
      bool sendOtherForces)
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

      this.sendBeamForces = sendBeamForces;
      this.sendBraceForces = sendBraceForces;
      this.sendColumnForces = sendColumnForces;
      this.sendOtherForces = sendOtherForces;
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

    private ICollection<ResultSet1D> GetAnalysisResultsForElement1D(Element1D element1D)
    {
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

    private ICollection<ResultSet1D> GetAnalysisResultsForFrame(Element1D element1D)
    {
      int forcesSuccess = -1;

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

      if (SendForces(element1D.type))
      {
        forcesSuccess = sapModel.Results.FrameForce(
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
      }
      else
      {
        return new List<ResultSet1D>();
      }

      // Value used to normalized output station of forces between 0 and 1
      var lengthOf1dElement = objSta.Max();

      return CreateLoadCombinationResults(
        element1D,
        forcesSuccess,
        numberOfResults,
        null,
        loadCase,
        (int i) => (float)(objSta[i] / lengthOf1dElement),
        p,
        v2,
        v3,
        t,
        m2,
        m3);
    }
    private ResultSet1D GetOrCreateResult(Dictionary<string, ResultSet1D> dict, string loadCaseName)
    {
      if (!dict.TryGetValue(loadCaseName, out ResultSet1D comboResults))
      {
        Base loadCaseOrCombination = loadCombinationsAndCases[loadCaseName];
        comboResults = new ResultSet1D(new())
        {
          resultCase = loadCaseOrCombination
        };
        dict[loadCaseName] = comboResults;
      }
      return comboResults;
    }
    private ICollection<ResultSet1D> GetAnalysisResultsForPier(Element1D element1D)
    {
      int forcesSuccess = -1;

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

      if (SendForces(element1D.type))
      {
        forcesSuccess = sapModel.Results.PierForce(
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
      }

      return CreateLoadCombinationResults(
        element1D,
        forcesSuccess,
        numberOfResults,
        pierName,
        loadCase,
        Return0Position,
        p,
        v2,
        v3,
        t,
        m2,
        m3);
    }
    private ICollection<ResultSet1D> GetAnalysisResultsForSpandrel(Element1D element1D)
    {
      int forcesSuccess = -1;

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

      if (SendForces(element1D.type))
      {
        forcesSuccess = sapModel.Results.SpandrelForce(
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
      }

      return CreateLoadCombinationResults(
        element1D, 
        forcesSuccess, 
        numberOfResults, 
        spandrelName, 
        loadCase,
        Return0Position,
        p, 
        v2, 
        v3, 
        t, 
        m2, 
        m3);
    }

    private ICollection<ResultSet1D> CreateLoadCombinationResults(
      Element1D element1D,
      int forcesSuccess,
      int numberOfResults,
      string[] names,
      string[] loadCase,
      Func<int, float> positionCalculator, 
      double[] p,
      double[] v2,
      double[] v3,
      double[] t,
      double[] m2,
      double[] m3)
    {
      Dictionary<string, ResultSet1D> loadCombinationResults = new();
      for (int i = 0; i < numberOfResults; i++)
      {
        if (names != null && names[i] != element1D.name)
        {
          continue;
        }
        Result1D result = new();

        if (forcesSuccess.IsSuccessful())
        {
          result.position = positionCalculator(i);
          result.forceX = (float)p[i];
          result.forceY = (float)v2[i];
          result.forceZ = (float)v3[i];
          result.momentXX = (float)t[i];
          result.momentYY = (float)m2[i];
          result.momentZZ = (float)m3[i];
        };
        GetOrCreateResult(loadCombinationResults, loadCase[i]).results1D.Add(result);
      }
      return loadCombinationResults.Values;
    }

    private bool SendForces(ElementType1D type)
    {
      return type switch
      {
        ElementType1D.Beam => sendBeamForces,
        ElementType1D.Brace => sendBraceForces,
        ElementType1D.Column => sendColumnForces,
        _ => sendOtherForces
      };
    }

    private float Return0Position(int i)
    {
      return 0;
    }
  }
}

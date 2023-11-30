#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Results;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConverterCSIShared.Models;

internal class Element1DAnalyticalResultConverter
{
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
    cSapModel sapModel,
    HashSet<string> frameNames,
    HashSet<string> pierNames,
    HashSet<string> spandrelNames,
    IEnumerable<LoadCombination> loadCombinations,
    IEnumerable<LoadCase> loadCases,
    bool sendBeamForces,
    bool sendBraceForces,
    bool sendColumnForces,
    bool sendOtherForces
  )
  {
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

  public AnalyticalResults? AnalyticalResultsToSpeckle(string elementName, ElementType1D elementType)
  {
    if (SendForces(elementType))
    {
      return new() { resultsByLoadCombination = GetAnalysisResultsForElement1D(elementName).Cast<Result>().ToList() };
    }
    return null;
  }

  private ICollection<ResultSet1D> GetAnalysisResultsForElement1D(string elementName)
  {
    if (frameNames.Contains(elementName))
    {
      return GetAnalysisResultsForFrame(elementName);
    }

    if (pierNames.Contains(elementName))
    {
      return GetAnalysisResultsForPier(elementName);
    }

    if (spandrelNames.Contains(elementName))
    {
      return GetAnalysisResultsForSpandrel(elementName);
    }

    throw new SpeckleException($"Unable to find category for Element1D with name {elementName}");
  }

  private ICollection<ResultSet1D> GetAnalysisResultsForFrame(string elementName)
  {
    int forcesSuccess = -1;

    // Reference variables for CSI API
    int numberOfResults = 0;
    var obj = Array.Empty<string>();
    var elm = Array.Empty<string>();
    var loadCase = Array.Empty<string>();
    var stepType = Array.Empty<string>();
    var objSta = Array.Empty<double>();
    var elmSta = Array.Empty<double>();
    var stepNum = Array.Empty<double>();
    var p = Array.Empty<double>();
    var v2 = Array.Empty<double>();
    var v3 = Array.Empty<double>();
    var t = Array.Empty<double>();
    var m2 = Array.Empty<double>();
    var m3 = Array.Empty<double>();

    forcesSuccess = sapModel.Results.FrameForce(
      elementName,
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
    double lengthOf1dElement = objSta.Max();

    return CreateLoadCombinationResults(
      elementName,
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
      m3
    );
  }

  private ResultSet1D GetOrCreateResult(Dictionary<string, ResultSet1D> dict, string loadCaseName)
  {
    if (!dict.TryGetValue(loadCaseName, out ResultSet1D comboResults))
    {
      Base loadCaseOrCombination = loadCombinationsAndCases[loadCaseName];
      comboResults = new ResultSet1D(new()) { resultCase = loadCaseOrCombination };
      dict[loadCaseName] = comboResults;
    }
    return comboResults;
  }

  private ICollection<ResultSet1D> GetAnalysisResultsForPier(string elementName)
  {
    int forcesSuccess = -1;

    // Reference variables for CSI API
    int numberOfResults = 0;
    var storyName = Array.Empty<string>();
    var pierName = Array.Empty<string>();
    var loadCase = Array.Empty<string>();
    var location = Array.Empty<string>();
    var p = Array.Empty<double>();
    var v2 = Array.Empty<double>();
    var v3 = Array.Empty<double>();
    var t = Array.Empty<double>();
    var m2 = Array.Empty<double>();
    var m3 = Array.Empty<double>();

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

    return CreateLoadCombinationResults(
      elementName,
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
      m3
    );

    // local function that just returns 0 in order to avoid unnecessary heap allocations
    // that would occur if we were using a lambda
    static float Return0Position(int i)
    {
      return 0;
    }
  }

  private ICollection<ResultSet1D> GetAnalysisResultsForSpandrel(string elementName)
  {
    int forcesSuccess = -1;

    // Reference variables for CSI API
    int numberOfResults = 0;
    var storyName = Array.Empty<string>();
    var spandrelName = Array.Empty<string>();
    var loadCase = Array.Empty<string>();
    var location = Array.Empty<string>();
    var p = Array.Empty<double>();
    var v2 = Array.Empty<double>();
    var v3 = Array.Empty<double>();
    var t = Array.Empty<double>();
    var m2 = Array.Empty<double>();
    var m3 = Array.Empty<double>();

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

    return CreateLoadCombinationResults(
      elementName,
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
      m3
    );

    // local function that just returns 0 in order to avoid unnecessary heap allocations
    // that would occur if we were using a lambda
    static float Return0Position(int i)
    {
      return 0;
    }
  }

  private ICollection<ResultSet1D> CreateLoadCombinationResults(
    string elementName,
    int forcesSuccess,
    int numberOfResults,
    string[]? names,
    string[] loadCase,
    Func<int, float> positionCalculator,
    double[] p,
    double[] v2,
    double[] v3,
    double[] t,
    double[] m2,
    double[] m3
  )
  {
    Dictionary<string, ResultSet1D> loadCombinationResults = new();
    for (int i = 0; i < numberOfResults; i++)
    {
      if (names != null && names[i] != elementName)
      {
        continue;
      }
      Result1D result = new();

      if (ApiResultValidator.IsSuccessful(forcesSuccess))
      {
        result.position = positionCalculator(i);
        result.forceX = (float)p[i];
        result.forceY = (float)v2[i];
        result.forceZ = (float)v3[i];
        result.momentXX = (float)t[i];
        result.momentYY = (float)m2[i];
        result.momentZZ = (float)m3[i];
      }
      ;
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
}

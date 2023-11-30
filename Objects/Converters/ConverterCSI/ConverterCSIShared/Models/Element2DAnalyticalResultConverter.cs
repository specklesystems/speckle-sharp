#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Loading;
using Objects.Structural.Results;
using Speckle.Core.Models;

namespace ConverterCSIShared.Models;

internal class Element2DAnalyticalResultConverter
{
  private readonly cSapModel sapModel;
  private readonly Dictionary<string, Base> loadCombinationsAndCases;
  private readonly bool sendForces;
  private readonly bool sendStresses;

  public Element2DAnalyticalResultConverter(
    cSapModel sapModel,
    IEnumerable<LoadCombination> loadCombinations,
    IEnumerable<LoadCase> loadCases,
    bool sendForces,
    bool sendStresses
  )
  {
    this.sapModel = sapModel;
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

    this.sendForces = sendForces;
    this.sendStresses = sendStresses;
  }

  public AnalyticalResults AnalyticalResultsToSpeckle(string areaName)
  {
    return new() { resultsByLoadCombination = GetAnalysisResultsForElement2D(areaName).Cast<Result>().ToList() };
  }

  private ICollection<ResultSet2D> GetAnalysisResultsForElement2D(string areaName)
  {
    int forceSuccess = -1;
    int stressSuccess = -1;

    int numberOfForceResults = 0;
    string[] obj = Array.Empty<string>();
    string[] elm = Array.Empty<string>();
    string[] pointElm = Array.Empty<string>();
    string[] loadCase = Array.Empty<string>();
    string[] stepType = Array.Empty<string>();
    double[] stepNum = Array.Empty<double>();
    double[] f11 = Array.Empty<double>();
    double[] f22 = Array.Empty<double>();
    double[] f12 = Array.Empty<double>();
    double[] fMax = Array.Empty<double>();
    double[] fMin = Array.Empty<double>();
    double[] fAngle = Array.Empty<double>();
    double[] fVonMises = Array.Empty<double>();
    double[] m11 = Array.Empty<double>();
    double[] m22 = Array.Empty<double>();
    double[] m12 = Array.Empty<double>();
    double[] mMax = Array.Empty<double>();
    double[] mMin = Array.Empty<double>();
    double[] mAngle = Array.Empty<double>();
    double[] v13 = Array.Empty<double>();
    double[] v23 = Array.Empty<double>();
    double[] vMax = Array.Empty<double>();
    double[] vAngle = Array.Empty<double>();

    if (sendForces)
    {
      forceSuccess = sapModel.Results.AreaForceShell(
        areaName,
        CSiAPIv1.eItemTypeElm.ObjectElm,
        ref numberOfForceResults,
        ref obj,
        ref elm,
        ref pointElm,
        ref loadCase,
        ref stepType,
        ref stepNum,
        ref f11,
        ref f22,
        ref f12,
        ref fMax,
        ref fMin,
        ref fAngle,
        ref fVonMises,
        ref m11,
        ref m22,
        ref m12,
        ref mMax,
        ref mMin,
        ref mAngle,
        ref v13,
        ref v23,
        ref vMax,
        ref vAngle
      );
    }

    int numberOfStressResults = 0;
    string[] stressObj = Array.Empty<string>();
    string[] stressElm = Array.Empty<string>();
    string[] stressPointElm = Array.Empty<string>();
    string[] stressLoadCase = Array.Empty<string>();
    string[] stressStepType = Array.Empty<string>();
    double[] stressStepNum = Array.Empty<double>();
    double[] S11Top = Array.Empty<double>();
    double[] S22Top = Array.Empty<double>();
    double[] S12Top = Array.Empty<double>();
    double[] SMaxTop = Array.Empty<double>();
    double[] SMinTop = Array.Empty<double>();
    double[] SAngleTop = Array.Empty<double>();
    double[] sVonMisesTop = Array.Empty<double>();
    double[] S11Bot = Array.Empty<double>();
    double[] S22Bot = Array.Empty<double>();
    double[] S12Bot = Array.Empty<double>();
    double[] SMaxBot = Array.Empty<double>();
    double[] SMinBot = Array.Empty<double>();
    double[] SAngleBot = Array.Empty<double>();
    double[] sVonMisesBot = Array.Empty<double>();
    double[] S13Avg = Array.Empty<double>();
    double[] S23Avg = Array.Empty<double>();
    double[] SMaxAvg = Array.Empty<double>();
    double[] SAngleAvg = Array.Empty<double>();

    if (sendStresses)
    {
      stressSuccess = sapModel.Results.AreaStressShell(
        areaName,
        CSiAPIv1.eItemTypeElm.ObjectElm,
        ref numberOfStressResults,
        ref stressObj,
        ref stressElm,
        ref stressPointElm,
        ref stressLoadCase,
        ref stressStepType,
        ref stressStepNum,
        ref S11Top,
        ref S22Top,
        ref S12Top,
        ref SMaxTop,
        ref SMinTop,
        ref SAngleTop,
        ref sVonMisesTop,
        ref S11Bot,
        ref S22Bot,
        ref S12Bot,
        ref SMaxBot,
        ref SMinBot,
        ref SAngleBot,
        ref sVonMisesBot,
        ref S13Avg,
        ref S23Avg,
        ref SMaxAvg,
        ref SAngleAvg
      );
    }

    Dictionary<string, ResultSet2D> resultSets = new();
    for (int i = 0; i < numberOfForceResults; i++)
    {
      Result2D speckleResult2D = new();

      if (ApiResultValidator.IsSuccessful(forceSuccess))
      {
        speckleResult2D.forceXX = (float)f11[i];
        speckleResult2D.forceYY = (float)f22[i];
        speckleResult2D.forceXY = (float)f12[i];
        speckleResult2D.momentXX = (float)m11[i];
        speckleResult2D.momentYY = (float)m22[i];
        speckleResult2D.momentXY = (float)m12[i];
        speckleResult2D.shearX = (float)v13[i];
        speckleResult2D.shearY = (float)v23[i];
      }

      if (ApiResultValidator.IsSuccessful(stressSuccess))
      {
        speckleResult2D.stressTopXX = (float)S11Top[i];
        speckleResult2D.stressTopYY = (float)S22Top[i];
        speckleResult2D.stressTopZZ = 0; // shell elements are 2D elements
        speckleResult2D.stressTopXY = (float)S12Top[i];
        speckleResult2D.stressTopYZ = (float)S23Avg[i]; // CSI reports avg out-of-plane shear
        speckleResult2D.stressTopZX = (float)S12Top[i];
        speckleResult2D.stressBotXX = (float)S11Bot[i];
        speckleResult2D.stressBotYY = (float)S22Bot[i];
        speckleResult2D.stressBotZZ = 0; // shell elements are 2D elements
        speckleResult2D.stressBotXY = (float)S12Bot[i];
        speckleResult2D.stressBotYZ = (float)S23Avg[i]; // CSI reports avg out-of-plane shear
        speckleResult2D.stressBotZX = (float)S12Bot[i];
      }
      GetOrCreateResult(resultSets, loadCase[i]).results2D.Add(speckleResult2D);
    }

    return resultSets.Values;
  }

  private ResultSet2D GetOrCreateResult(Dictionary<string, ResultSet2D> dict, string loadCaseName)
  {
    if (!dict.TryGetValue(loadCaseName, out ResultSet2D comboResults))
    {
      Base loadCaseOrCombination = loadCombinationsAndCases[loadCaseName];
      comboResults = new ResultSet2D(new()) { resultCase = loadCaseOrCombination };
      dict[loadCaseName] = comboResults;
    }
    return comboResults;
  }
}

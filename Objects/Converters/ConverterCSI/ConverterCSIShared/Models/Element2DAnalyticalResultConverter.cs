using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Results;
using Speckle.Core.Models;

namespace ConverterCSIShared.Models
{
  internal class Element2DAnalyticalResultConverter
  {
    private readonly Model speckleModel;
    private readonly cSapModel sapModel;
    private readonly Dictionary<string, Base> loadCombinationsAndCases;
    public Element2DAnalyticalResultConverter(
      Model speckleModel,
      cSapModel sapModel,
      IEnumerable<LoadCombination> loadCombinations,
      IEnumerable<LoadCase> loadCases
    )
    {
      this.speckleModel = speckleModel;
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
    }

    public void AnalyticalResultsToSpeckle()
    {
      foreach (Base element in speckleModel.elements)
      {
        if (element is not CSIElement2D element2D)
        {
          continue;
        }

        AnalyticalResults results = new()
        {
          resultsByLoadCombination = GetAnalysisResultsForElement2D(element2D).Cast<Result>().ToList()
        };
        element2D.AnalysisResults = results;
      }
    }

    private ICollection<ResultSet2D> GetAnalysisResultsForElement2D(Element2D element2D)
    {
      int numberOfForceResults = 0;
      string[] obj,
        elm,
        pointElm,
        loadCase,
        stepType;
      double[] stepNum,
        f11,
        f22,
        f12,
        fMax,
        fMin,
        fAngle,
        fVonMises,
        m11,
        m22,
        m12,
        mMax,
        mMin,
        mAngle,
        v13,
        v23,
        vMax,
        vAngle;
      obj = elm = pointElm = loadCase = stepType = Array.Empty<string>();
      stepNum =
        f11 =
        f22 =
        f12 =
        fMax =
        fMin =
        fAngle =
        fVonMises =
        m11 =
        m22 =
        m12 =
        mMax =
        mMin =
        mAngle =
        v13 =
        v23 =
        vMax =
        vAngle =
          Array.Empty<double>();

      sapModel.Results.AreaForceShell(
        element2D.name,
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

      int numberOfStressResults = 0;
      string[] stressObj,
        stressElm,
        stressPointElm,
        stressLoadCase,
        stressStepType;
      double[] stressStepNum,
        S11Top,
        S22Top,
        S12Top,
        SMaxTop,
        SMinTop,
        SAngleTop,
        sVonMisesTop,
        S11Bot,
        S22Bot,
        S12Bot,
        SMaxBot,
        SMinBot,
        SAngleBot,
        sVonMisesBot,
        S13Avg,
        S23Avg,
        SMaxAvg,
        SAngleAvg;
      stressObj = stressElm = stressPointElm = stressLoadCase = stressStepType = Array.Empty<string>();
      stressStepNum =
        S11Top =
        S22Top =
        S12Top =
        SMaxTop =
        SMinTop =
        SAngleTop =
        sVonMisesTop =
        S11Bot =
        S22Bot =
        S12Bot =
        SMaxBot =
        SMinBot =
        SAngleBot =
        sVonMisesBot =
        S13Avg =
        S23Avg =
        SMaxAvg =
        SAngleAvg =
          Array.Empty<double>();

      sapModel.Results.AreaStressShell(
        element2D.name,
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

      Dictionary<string, ResultSet2D> resultSets = new();
      for (int i = 0; i < numberOfForceResults; i++)
      {

        Result2D speckleResult2D = new()
        {
          position = new List<double>(),
          dispX = 0, // pulling this data would require large amount of data parsing, implementation TBD
          dispY = 0, // pulling this data would require large amount of data parsing, implementation TBD
          dispZ = 0, // pulling this data would require large amount of data parsing, implementation TBD
          forceXX = (float)f11[i],
          forceYY = (float)f22[i],
          forceXY = (float)f12[i],
          momentXX = (float)m11[i],
          momentYY = (float)m22[i],
          momentXY = (float)m12[i],
          shearX = (float)v13[i],
          shearY = (float)v23[i],
          stressTopXX = (float)S11Top[i],
          stressTopYY = (float)S22Top[i],
          stressTopZZ = 0, // shell elements are 2D elements
          stressTopXY = (float)S12Top[i],
          stressTopYZ = (float)S23Avg[i], // CSI reports avg out-of-plane shear
          stressTopZX = (float)S12Top[i],
          stressMidXX = 0, // CSI does not report
          stressMidYY = 0, // CSI does not report
          stressMidZZ = 0, // CSI does not report
          stressMidXY = 0, // CSI does not report
          stressMidYZ = 0, // CSI does not report
          stressMidZX = 0, // CSI does not report
          stressBotXX = (float)S11Bot[i],
          stressBotYY = (float)S22Bot[i],
          stressBotZZ = 0, // shell elements are 2D elements
          stressBotXY = (float)S12Bot[i],
          stressBotYZ = (float)S23Avg[i], // CSI reports avg out-of-plane shear
          stressBotZX = (float)S12Bot[i],
        };
        GetOrCreateResult(resultSets, loadCase[i]).results2D.Add(speckleResult2D);
      }

      return resultSets.Values;
    }

    private ResultSet2D GetOrCreateResult(Dictionary<string, ResultSet2D> dict, string loadCaseName)
    {
      if (!dict.TryGetValue(loadCaseName, out ResultSet2D comboResults))
      {
        Base loadCaseOrCombination = loadCombinationsAndCases[loadCaseName];
        comboResults = new ResultSet2D(new())
        {
          resultCase = loadCaseOrCombination
        };
        dict[loadCaseName] = comboResults;
      }
      return comboResults;
    }
  }
}

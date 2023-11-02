using System;
using System.Collections.Generic;
using System.Linq;
using ConverterCSIShared.Extensions;
using CSiAPIv1;
using Objects.Structural.Loading;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public LoadCombination LoadCombinationToSpeckle(string loadComboName)
    {
      return aggregateCache
        .GetOrInitializeEmptyCacheOfType<LoadCombination>()
        .GetOrAdd(loadComboName, CreateLoadCombination);
    }

    private LoadCombination CreateLoadCombination(string loadComboName)
    {
      int numItems = 0;
      eCNameType[] cNameTypes = null;
      string[] loadCaseNames = null;
      double[] scaleFactors = null;
      Model.RespCombo.GetCaseList(
        loadComboName,
        ref numItems,
        ref cNameTypes,
        ref loadCaseNames,
        ref scaleFactors)
        .ThrowIfUnsuccessful($"Unable to get load cases for load combination named {loadComboName}");

      List<double> factors = new();
      List<LoadCase> loadCases = new();
      for (int i = 0; i < numItems; i++)
      {
        string loadCaseName = loadCaseNames[i];
        LoadCase loadCase = LoadPatternToSpeckle(loadCaseName);

        factors.Add(scaleFactors[i]);
        loadCases.Add(loadCase);
      }
      CombinationType type = GetCombinationType(loadComboName);

      return new LoadCombination(loadComboName, loadCases.Cast<Base>().ToList(), factors, type);
    }

    private CombinationType GetCombinationType(string loadComboName)
    {
      int comboType = 0;
      if (!Model.RespCombo.GetTypeCombo(loadComboName, ref comboType).IsSuccessful())
      {
        // todo : add default (unset) value for this enum?
        return CombinationType.LinearAdd;
      }

      return comboType switch
      {
        0 => CombinationType.LinearAdd,
        1 => CombinationType.Envelope,
        2 => CombinationType.AbsoluteAdd,
        3 => CombinationType.SRSS,
        4 => CombinationType.RangeAdd,
        _ => CombinationType.LinearAdd
      };
    }
  }
}

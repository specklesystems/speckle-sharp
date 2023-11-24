using Objects.Structural.Loading;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public Base ResultsToSpeckle()
  {
    return ResultGlobal();
  }

  private IEnumerable<LoadCase> GetLoadCases()
  {
    var numberOfLoadCases = 0;
    var loadCaseNames = Array.Empty<string>();

    Model.LoadCases.GetNameList(ref numberOfLoadCases, ref loadCaseNames);
    foreach (var loadCase in loadCaseNames)
    {
      yield return LoadPatternToSpeckle(loadCase);
    }
  }

  private IEnumerable<LoadCombination> GetLoadCombos()
  {
    var numberOfLoadCombos = 0;
    var loadComboNames = Array.Empty<string>();

    Model.RespCombo.GetNameList(ref numberOfLoadCombos, ref loadComboNames);
    foreach (string loadComboName in loadComboNames)
    {
      yield return LoadCombinationToSpeckle(loadComboName);
    }
  }
}

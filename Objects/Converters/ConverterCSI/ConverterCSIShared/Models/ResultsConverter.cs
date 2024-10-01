#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Loading;

namespace ConverterCSIShared.Models;

internal sealed class ResultsConverter
{
  public ResultsConverter(
    cSapModel sapModel,
    Dictionary<string, string> settings,
    IEnumerable<LoadCase> loadCases,
    IEnumerable<LoadCombination> loadCombinations
  )
  {
    SetLoadCombinationsForResults(sapModel, settings);

    DefineNodeResultsConverter(sapModel, settings, loadCases, loadCombinations);
    DefineElement1DResultsConverter(sapModel, settings, loadCases, loadCombinations);
    DefineElement2DResultsConverter(sapModel, settings, loadCases, loadCombinations);
  }

  public NodeAnalyticalResultsConverter? NodeAnalyticalResultsConverter { get; private set; }
  public Element1DAnalyticalResultConverter? Element1DAnalyticalResultConverter { get; private set; }
  public Element2DAnalyticalResultConverter? Element2DAnalyticalResultConverter { get; private set; }

  private void DefineElement2DResultsConverter(
    cSapModel sapModel,
    Dictionary<string, string> settings,
    IEnumerable<LoadCase> loadCases,
    IEnumerable<LoadCombination> loadCombinations
  )
  {
    if (
      settings.TryGetValue(Constants.RESULTS_2D_SLUG, out var selection2D)
      && !string.IsNullOrEmpty(selection2D)
      && selection2D.Split(',') is string[] results2DToSend
    )
    {
      Element2DAnalyticalResultConverter = new(
        sapModel,
        loadCombinations,
        loadCases,
        results2DToSend.Contains(Constants.FORCES),
        results2DToSend.Contains(Constants.STRESSES)
      );
    }
    else if (GetLegacyBoolSettingOrFalse(settings, Constants.LEGACY_SEND_2D_RESULTS))
    {
      Element2DAnalyticalResultConverter = new(sapModel, loadCombinations, loadCases, true, true);
    }
  }

  private void DefineElement1DResultsConverter(
    cSapModel sapModel,
    Dictionary<string, string> settings,
    IEnumerable<LoadCase> loadCases,
    IEnumerable<LoadCombination> loadCombinations
  )
  {
    if (
      settings.TryGetValue(Constants.RESULTS_1D_SLUG, out var selection1D)
      && !string.IsNullOrEmpty(selection1D)
      && selection1D.Split(new string[] { ", " }, StringSplitOptions.None) is string[] results1DToSend
    )
    {
      Element1DAnalyticalResultConverter = new(
        sapModel,
        new HashSet<string>(GetFrameNames(sapModel)),
        new HashSet<string>(GetPierNames(sapModel)),
        new HashSet<string>(GetSpandrelNames(sapModel)),
        loadCombinations,
        loadCases,
        results1DToSend.Contains(Constants.BEAM_FORCES),
        results1DToSend.Contains(Constants.BRACE_FORCES),
        results1DToSend.Contains(Constants.COLUMN_FORCES),
        results1DToSend.Contains(Constants.OTHER_FORCES)
      );
    }
    else if (GetLegacyBoolSettingOrFalse(settings, Constants.LEGACY_SEND_1D_RESULTS))
    {
      Element1DAnalyticalResultConverter = new(
        sapModel,
        new HashSet<string>(GetFrameNames(sapModel)),
        new HashSet<string>(GetPierNames(sapModel)),
        new HashSet<string>(GetSpandrelNames(sapModel)),
        loadCombinations,
        loadCases,
        true,
        true,
        true,
        true
      );
    }
  }

  private void DefineNodeResultsConverter(
    cSapModel sapModel,
    Dictionary<string, string> settings,
    IEnumerable<LoadCase> loadCases,
    IEnumerable<LoadCombination> loadCombinations
  )
  {
    if (
      settings.TryGetValue(Constants.RESULTS_NODE_SLUG, out var selection)
      && !string.IsNullOrEmpty(selection)
      && selection.Split(',') is string[] resultsToSend
    )
    {
      NodeAnalyticalResultsConverter = new(
        sapModel,
        loadCombinations,
        loadCases,
        resultsToSend.Contains(Constants.DISPLACEMENTS),
        resultsToSend.Contains(Constants.FORCES),
        resultsToSend.Contains(Constants.VELOCITIES),
        resultsToSend.Contains(Constants.ACCELERATIONS)
      );
    }
    else if (GetLegacyBoolSettingOrFalse(settings, Constants.LEGACY_SEND_NODE_RESULTS))
    {
      NodeAnalyticalResultsConverter = new(sapModel, loadCombinations, loadCases, true, true, true, true);
    }
  }

  public static void SetLoadCombinationsForResults(cSapModel sapModel, Dictionary<string, string> settings)
  {
    // because we switched the settings for allowing users to send results,
    // some users may still have stream cards with saved data from the old settings
    bool shouldSendAllLoadCases = ShouldSendResultsBasedOnLegacySettings(settings);

    sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
    if (
      !settings.TryGetValue(Constants.RESULTS_LOAD_CASES_SLUG, out string loadCasesCommaSeparated)
      || string.IsNullOrEmpty(loadCasesCommaSeparated)
    )
    {
      // if not checking for legacy settings then we could just exit here
      // return
      loadCasesCommaSeparated = string.Empty;
    }

    string[] loadCases = loadCasesCommaSeparated.Split(',').Select(s => s.TrimStart()).ToArray();

    var numberOfLoadCombinations = 0;
    var loadCombinationNames = Array.Empty<string>();

    sapModel.RespCombo.GetNameList(ref numberOfLoadCombinations, ref loadCombinationNames);
    foreach (var loadCombination in loadCombinationNames)
    {
      if (loadCases.Contains(loadCombination) || shouldSendAllLoadCases)
      {
        sapModel.Results.Setup.SetComboSelectedForOutput(loadCombination);
      }
    }

    sapModel.LoadCases.GetNameList(ref numberOfLoadCombinations, ref loadCombinationNames);
    foreach (var loadCase in loadCombinationNames)
    {
      if (loadCases.Contains(loadCase) || shouldSendAllLoadCases)
      {
        sapModel.Results.Setup.SetCaseSelectedForOutput(loadCase);
      }
    }
  }

  private static bool ShouldSendResultsBasedOnLegacySettings(Dictionary<string, string> settings)
  {
    bool legacySendNodes = GetLegacyBoolSettingOrFalse(settings, Constants.LEGACY_SEND_NODE_RESULTS);
    bool legacySendElement1D = GetLegacyBoolSettingOrFalse(settings, Constants.LEGACY_SEND_1D_RESULTS);
    bool legacySendElement2D = GetLegacyBoolSettingOrFalse(settings, Constants.LEGACY_SEND_2D_RESULTS);

    return legacySendNodes || legacySendElement1D || legacySendElement2D;
  }

  private static bool GetLegacyBoolSettingOrFalse(Dictionary<string, string> settings, string slug)
  {
    if (settings.TryGetValue(slug, out string stringValue) && bool.TryParse(stringValue, out bool value))
    {
      return value;
    }
    return false;
  }

  private static string[] GetFrameNames(cSapModel sapModel)
  {
    int numberOfFrameNames = 0;
    var frameNames = Array.Empty<string>();

    sapModel.FrameObj.GetNameList(ref numberOfFrameNames, ref frameNames);
    return frameNames;
  }

  private static string[] GetPierNames(cSapModel sapModel)
  {
    int numberOfNames = 0;
    var pierNames = Array.Empty<string>();

    sapModel.PierLabel.GetNameList(ref numberOfNames, ref pierNames);
    return pierNames;
  }

  private static string[] GetSpandrelNames(cSapModel sapModel)
  {
    int numberOfSpandrelNames = 0;
    var spandrelNames = Array.Empty<string>();
    var isMultiStory = Array.Empty<bool>();

    sapModel.SpandrelLabel.GetNameList(ref numberOfSpandrelNames, ref spandrelNames, ref isMultiStory);
    return spandrelNames;
  }
}

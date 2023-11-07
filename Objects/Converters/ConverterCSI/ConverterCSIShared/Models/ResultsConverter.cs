#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using Objects.Structural.Loading;

namespace ConverterCSIShared.Models
{
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

    private void DefineElement2DResultsConverter(cSapModel sapModel, Dictionary<string, string> settings, IEnumerable<LoadCase> loadCases, IEnumerable<LoadCombination> loadCombinations)
    {
      if (settings.TryGetValue(Constants.Results2dSlug, out var selection2D)
            && !string.IsNullOrEmpty(selection2D)
            && selection2D.Split(',') is string[] results2DToSend)
      {
        Element2DAnalyticalResultConverter = new(
          sapModel,
          loadCombinations,
          loadCases,
          results2DToSend.Contains(Constants.Forces),
          results2DToSend.Contains(Constants.Stresses));
      }
      else if (GetLegacyBoolSettingOrFalse(settings, Constants.LegacySend2DResults))
      {
        Element2DAnalyticalResultConverter = new(
          sapModel,
          loadCombinations,
          loadCases,
          true,
          true);
      }
    }

    private void DefineElement1DResultsConverter(cSapModel sapModel, Dictionary<string, string> settings, IEnumerable<LoadCase> loadCases, IEnumerable<LoadCombination> loadCombinations)
    {
      if (settings.TryGetValue(Constants.Results1dSlug, out var selection1D)
            && !string.IsNullOrEmpty(selection1D)
            && selection1D.Split(new string[] { ", " }, StringSplitOptions.None) is string[] results1DToSend)
      {
        Element1DAnalyticalResultConverter = new(
          sapModel,
          new HashSet<string>(GetFrameNames(sapModel)),
          new HashSet<string>(GetPierNames(sapModel)),
          new HashSet<string>(GetSpandrelNames(sapModel)),
          loadCombinations,
          loadCases,
          results1DToSend.Contains(Constants.BeamForces),
          results1DToSend.Contains(Constants.BraceForces),
          results1DToSend.Contains(Constants.ColumnForces),
          results1DToSend.Contains(Constants.OtherForces));
      }
      else if (GetLegacyBoolSettingOrFalse(settings, Constants.LegacySend1DResults))
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
          true);
      }
    }

    private void DefineNodeResultsConverter(cSapModel sapModel, Dictionary<string, string> settings, IEnumerable<LoadCase> loadCases, IEnumerable<LoadCombination> loadCombinations)
    {
      if (settings.TryGetValue(Constants.ResultsNodeSlug, out var selection)
            && !string.IsNullOrEmpty(selection)
            && selection.Split(',') is string[] resultsToSend)
      {
        NodeAnalyticalResultsConverter = new(
          sapModel,
          loadCombinations,
          loadCases,
          resultsToSend.Contains(Constants.Displacements),
          resultsToSend.Contains(Constants.Forces),
          resultsToSend.Contains(Constants.Velocities),
          resultsToSend.Contains(Constants.Accelerations));
      }
      else if (GetLegacyBoolSettingOrFalse(settings, Constants.LegacySendNodeResults))
      {
        NodeAnalyticalResultsConverter = new(
          sapModel,
          loadCombinations,
          loadCases,
          true,
          true,
          true,
          true);
      }
    }

    public static void SetLoadCombinationsForResults(cSapModel sapModel, Dictionary<string, string> settings)
    {
      // because we switched the settings for allowing users to send results,
      // some users may still have stream cards with saved data from the old settings
      bool shouldSendAllLoadCases = ShouldSendResultsBasedOnLegacySettings(settings);
      
      sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
      if (!settings.TryGetValue("load-cases", out string loadCasesCommaSeparated)
        || string.IsNullOrEmpty(loadCasesCommaSeparated))
      {
        // if not checking for legacy settings then we could just exit here
        // return
        loadCasesCommaSeparated = string.Empty;
      }

      string[] loadCases = loadCasesCommaSeparated.Split(',');

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
      bool legacySendNodes = GetLegacyBoolSettingOrFalse(settings, Constants.LegacySendNodeResults);
      bool legacySendElement1D = GetLegacyBoolSettingOrFalse(settings, Constants.LegacySend1DResults);
      bool legacySendElement2D = GetLegacyBoolSettingOrFalse(settings, Constants.LegacySend2DResults);

      return legacySendNodes || legacySendElement1D || legacySendElement2D;
    }

    private static bool GetLegacyBoolSettingOrFalse(
      Dictionary<string, string> settings,
      string slug)
    {
      if (settings.TryGetValue(slug, out string stringValue)
        && bool.TryParse(stringValue, out bool value)) 
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
}

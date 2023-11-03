using ConverterCSIShared;
using ConverterCSIShared.Models;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void ResultsToSpeckle()
    {
      SetLoadCombinationsForResults();
      List<LoadCase> loadCases = GetLoadCases().ToList();
      List<LoadCombination> loadCombos = GetLoadCombos().ToList();

      if (Settings.TryGetValue(Constants.ResultsNodeSlug, out var selection) 
        && !string.IsNullOrEmpty(selection)
        && selection.Split(',') is string[] resultsToSend)
      {
        NodeAnalyticalResultsConverter resultsConverter = new(
          SpeckleModel,
          Model,
          loadCombos,
          loadCases,
          resultsToSend.Contains(Constants.Displacements),
          resultsToSend.Contains(Constants.Forces),
          resultsToSend.Contains(Constants.Velocities),
          resultsToSend.Contains(Constants.Accelerations));
        resultsConverter.AnalyticalResultsToSpeckle();
      }

      if (Settings.TryGetValue(Constants.Results1dSlug, out var selection1D)
        && !string.IsNullOrEmpty(selection1D)
        && selection1D.Split(new string[] {", "}, StringSplitOptions.None) is string[] results1DToSend)
      {
        Element1DAnalyticalResultConverter resultsConverter = new(
          SpeckleModel,
          Model,
          new HashSet<string>(GetFrameNames()),
          new HashSet<string>(GetPierNames()),
          new HashSet<string>(GetSpandrelNames()),
          loadCombos,
          loadCases,
          results1DToSend.Contains(Constants.BeamForces),
          results1DToSend.Contains(Constants.BraceForces),
          results1DToSend.Contains(Constants.ColumnForces),
          results1DToSend.Contains(Constants.OtherForces)
          );
        resultsConverter.AnalyticalResultsToSpeckle();
      }

      if (Settings.TryGetValue(Constants.Results2dSlug, out var selection2D)
        && !string.IsNullOrEmpty(selection2D)
        && selection2D.Split(',') is string[] results2DToSend)
      {
        Element2DAnalyticalResultConverter resultsConverter = new(
          SpeckleModel,
          Model,
          loadCombos,
          loadCases,
          results2DToSend.Contains(Constants.Forces),
          results2DToSend.Contains(Constants.Stresses));
        resultsConverter.AnalyticalResultsToSpeckle();
      }
    }
    private string[] GetFrameNames()
    {
      int numberOfFrameNames = 0;
      var frameNames = Array.Empty<string>();

      Model.FrameObj.GetNameList(ref numberOfFrameNames, ref frameNames);
      return frameNames;
    }
    
    private string[] GetPierNames()
    {
      int numberOfNames = 0;
      var pierNames = Array.Empty<string>();

      Model.PierLabel.GetNameList(ref numberOfNames, ref pierNames);
      return pierNames;
    }
    
    private string[] GetSpandrelNames()
    {
      int numberOfSpandrelNames = 0;
      var spandrelNames = Array.Empty<string>();
      var isMultiStory = Array.Empty<bool>();

      Model.SpandrelLabel.GetNameList(ref numberOfSpandrelNames, ref spandrelNames, ref isMultiStory);
      return spandrelNames;
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
}

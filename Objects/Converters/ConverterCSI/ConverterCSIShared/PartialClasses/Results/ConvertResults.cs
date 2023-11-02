using ConverterCSIShared.Models;
using Objects.Structural.Loading;
using System;
using System.Collections.Generic;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void ResultsToSpeckle()
    {
      if (Settings.TryGetValue(Send1DResults, out string send1DResults) && bool.Parse(send1DResults))
      {
        Element1DAnalyticalResultConverter resultsConverter = new(
          SpeckleModel,
          Model,
          new HashSet<string>(GetFrameNames()),
          new HashSet<string>(GetPierNames()),
          new HashSet<string>(GetSpandrelNames()),
          GetLoadCombos(),
          GetLoadCases()
          );
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
    //public Base ResultsToSpeckle()
    //{
    //  var sendNodeResults = false;
    //  var send1DResults = false;
    //  var send2DResults = false;
    //  if (Settings.ContainsKey(SendNodeResults))
    //    bool.TryParse(Settings[SendNodeResults], out sendNodeResults);
    //  if (Settings.ContainsKey(Send1DResults))
    //    bool.TryParse(Settings[Send1DResults], out send1DResults);
    //  if (Settings.ContainsKey(Send2DResults))
    //    bool.TryParse(Settings[Send2DResults], out send2DResults);

    //  if (!sendNodeResults && !send1DResults && !send2DResults)
    //  {
    //    var resultsAll = new Base();
    //    resultsAll["Data"] =
    //      "Did not send any analytical results to Speckle. To send results, change the settings in the \"Advanced Settings\" tab";
    //    return resultsAll;
    //  }

    //  #region Retrieve frame names
    //  int numberOfFrameNames = 0;
    //  var frameNames = new string[] { };

    //  Model.FrameObj.GetNameList(ref numberOfFrameNames, ref frameNames);
    //  frameNames.ToList();
    //  List<string> convertedFrameNames = frameNames.ToList();
    //  #endregion

    //  #region Retrieve pier names

    //  int numberOfPierNames = 0;
    //  var pierNames = new string[] { };

    //  Model.PierLabel.GetNameList(ref numberOfPierNames, ref pierNames);
    //  List<string> convertedPierNames = pierNames.ToList();

    //  #endregion

    //  #region Retrieve spandrel names

    //  int numberOfSpandrelNames = 0;
    //  var spandrelNames = new string[] { };
    //  var isMultiStory = new bool[] { };

    //  Model.SpandrelLabel.GetNameList(ref numberOfSpandrelNames, ref spandrelNames, ref isMultiStory);
    //  List<string> convertedSpandrelNames = spandrelNames.ToList();

    //  #endregion

    //  #region Retrieve area names

    //  int numberOfAreaNames = 0;
    //  var areaNames = new string[] { };

    //  Model.AreaObj.GetNameList(ref numberOfAreaNames, ref areaNames);

    //  List<string> convertedAreaNames = areaNames.ToList();

    //  #endregion

    //  var resultsNode = sendNodeResults ? AllResultSetNodesToSpeckle() : null;
    //  var results1D = send1DResults
    //    ? AllResultSet1dToSpeckle(convertedFrameNames, convertedPierNames, convertedSpandrelNames)
    //    : null;
    //  var results2D = send2DResults ? AreaResultSet2dToSpeckle(convertedAreaNames) : null;

    //  var results = new ResultSetAll(results1D, results2D, new ResultSet3D(), new ResultGlobal(), resultsNode);

    //  return results;
    //}
  }
}

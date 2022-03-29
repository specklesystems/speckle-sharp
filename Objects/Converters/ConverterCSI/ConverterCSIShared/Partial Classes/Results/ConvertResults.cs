using CSiAPIv1;
using Objects.Structural.Geometry;
using Objects.Structural.Results;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {

    public ResultSetAll ResultsToSpeckle()
    {
      #region Retrieve frame names
      int numberOfFrameNames = 0;
      var frameNames = new string[] { };

      Model.FrameObj.GetNameList(ref numberOfFrameNames, ref frameNames);
      frameNames.ToList();
      List<string> convertedFrameNames = frameNames.ToList();
      #endregion

      #region Retrieve pier names

      int numberOfPierNames = 0;
      var pierNames = new string[] { };

      Model.PierLabel.GetNameList(ref numberOfPierNames, ref pierNames);
      List<string> convertedPierNames = pierNames.ToList();

      #endregion

      #region Retrieve spandrel names

      int numberOfSpandrelNames = 0;
      var spandrelNames = new string[] { };
      var isMultiStory = new bool[] { };

      Model.SpandrelLabel.GetNameList(ref numberOfSpandrelNames, ref spandrelNames, ref isMultiStory);
      List<string> convertedSpandrelNames = spandrelNames.ToList();


      #endregion

      #region Retrieve area names

      int numberOfAreaNames = 0;
      var areaNames = new string[] { };

      Model.AreaObj.GetNameList(ref numberOfAreaNames, ref areaNames);

      List<string> convertedAreaNames = areaNames.ToList();

      #endregion

      ResultSetAll results = new ResultSetAll(AllResultSet1dToSpeckle(convertedFrameNames, convertedPierNames, convertedSpandrelNames), AreaResultSet2dToSpeckle(convertedAreaNames), new ResultSet3D(), new ResultGlobal(), AllResultSetNodesToSpeckle());

      return results;
    }
  }

}
using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace ConnectorRevit.Revit
{
  public class ErrorEater : IFailuresPreprocessor
  {
    private ISpeckleConverter _converter;
    public ErrorEater(ISpeckleConverter converter)
    {
      _converter = converter;
    }
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
      IList<FailureMessageAccessor> failList = new List<FailureMessageAccessor>();
      // Inside event handler, get all warnings
      failList = failuresAccessor.GetFailureMessages();
      foreach (FailureMessageAccessor failure in failList)
      {
        // check FailureDefinitionIds against ones that you want to dismiss, 
        //FailureDefinitionId failID = failure.GetFailureDefinitionId();
        // prevent Revit from showing Unenclosed room warnings
        //if (failID == BuiltInFailures.RoomFailures.RoomNotEnclosed)
        //{
        var t = failure.GetDescriptionText();
        var r = failure.GetDefaultResolutionCaption();

        _converter.ConversionErrors.Add(new Error { message = t, details = "" });
      }

      failuresAccessor.DeleteAllWarnings();
      return FailureProcessingResult.Continue;
    }
  }
}

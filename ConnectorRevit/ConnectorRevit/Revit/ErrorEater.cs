using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Speckle.ConnectorRevit.Entry;
using Speckle.ConnectorRevit.UI;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
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
      var criticalFails = 0;
      var failedElements = new List<ElementId>();
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
        _converter.Report.LogConversionError(new Exception(t));

        var s = failure.GetSeverity();
        if (s == FailureSeverity.Warning)
          continue;
        try
        {
          failuresAccessor.ResolveFailure(failure);
        }
        catch (Exception ex)
        {
          // currently, the whole commit is rolled back. this should be investigated further at a later date
          // to properly proceed with commit
          failedElements.AddRange(failure.GetFailingElementIds());
          //_converter.ConversionErrors.Clear();
          var wrapped = new Exception(
            "Objects failed to bake due to a fatal error!\n"
              + "This is likely due to scaling issues - please ensure you've set the correct units on your objects or remove any invalid objects.\n\n"
              + "Revit error: "
              + t,
            ex
          );
          _converter.Report.LogConversionError(wrapped);
          SpeckleLog.Logger.Error(wrapped, "Revit commit failed {text}", t);
          return FailureProcessingResult.ProceedWithCommit;
        }
      }

      failuresAccessor.DeleteAllWarnings();
      return FailureProcessingResult.Continue;
    }
  }
}

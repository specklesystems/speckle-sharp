using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Speckle.Core.Logging;

namespace RevitSharedResources.Models;

public class ErrorEater : IFailuresPreprocessor
{
  private List<Exception> _exceptions = new();
  public Dictionary<string, int> CommitErrorsDict = new();

  public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
  {
    var resolvedFailures = 0;
    var failedElements = new List<ElementId>();
    // Inside event handler, get all warnings
    var failList = failuresAccessor.GetFailureMessages();
    foreach (FailureMessageAccessor failure in failList)
    {
      // check FailureDefinitionIds against ones that you want to dismiss,
      //FailureDefinitionId failID = failure.GetFailureDefinitionId();
      // prevent Revit from showing Unenclosed room warnings
      //if (failID == BuiltInFailures.RoomFailures.RoomNotEnclosed)
      //{
      var t = failure.GetDescriptionText();

      var s = failure.GetSeverity();
      if (s == FailureSeverity.Warning)
      {
        // just delete the warnings for now
        failuresAccessor.DeleteWarning(failure);
        resolvedFailures++;
        continue;
      }

      try
      {
        failuresAccessor.ResolveFailure(failure);
        resolvedFailures++;
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException ex)
      {
        var idsToDelete = failure.GetFailingElementIds().ToList();

        if (failuresAccessor.IsElementsDeletionPermitted(idsToDelete))
        {
          failuresAccessor.DeleteElements(idsToDelete);
          resolvedFailures++;
        }
        else
        {
          if (CommitErrorsDict.ContainsKey(t))
          {
            CommitErrorsDict[t]++;
          }
          else
          {
            CommitErrorsDict.Add(t, 1);
          }
          // currently, the whole commit is rolled back. this should be investigated further at a later date
          // to properly proceed with commit
          failedElements.AddRange(failure.GetFailingElementIds());

          // logging the error
          var speckleEx = new SpeckleException($"Unexpected error while preprocessing failures: {t}", ex);
          _exceptions.Add(speckleEx);
          SpeckleLog.Logger.Error(speckleEx, "Unexpected error while preprocessing failures: {failureMessage}", t);
        }
      }
    }

    if (resolvedFailures > 0)
    {
      return FailureProcessingResult.ProceedWithCommit;
    }
    else
    {
      return FailureProcessingResult.Continue;
    }
  }

  public SpeckleNonUserFacingException? GetException()
  {
    if (CommitErrorsDict.Count > 0 && _exceptions.Count > 0)
    {
      return new SpeckleNonUserFacingException(
        "Error eater was unable to resolve exceptions",
        new AggregateException(_exceptions)
      );
    }
    return null;
  }
}

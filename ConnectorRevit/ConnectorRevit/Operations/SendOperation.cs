using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia.Threading;
using ConnectorRevit.Services;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Revit.Async;
using RevitSharedResources.Interfaces;
using Serilog.Context;
using Speckle.ConnectorRevit;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConnectorRevit.Operations
{
  /// <summary>
  /// Creates and executes the roadmap for sending a selection of Revit objects as a Speckle commit.
  /// This is meant to be a transient service meaning that each send operation that the users does
  /// should instantiate a new instance of the <see cref="SendOperation"/>
  /// </summary>
  public class SendOperation
  {
    private readonly ISpeckleConverter converter;
    private readonly ISendSelection sendSelection;
    private readonly ISpeckleObjectSender speckleObjectSender;
    private readonly StreamState state;
    private readonly ProgressViewModel progress;
    private readonly UIDocument uiDocument;

    public SendOperation(
      ISpeckleConverter converter, 
      ISendSelection sendSelection, 
      ISpeckleObjectSender speckleObjectSender,
      IEntityProvider<UIDocument> uiDocumentProvider,
      IEntityProvider<StreamState> streamStateProvider,
      IEntityProvider<ProgressViewModel> progressProvider
    )
    {
      this.converter = converter;
      this.sendSelection = sendSelection;
      this.speckleObjectSender = speckleObjectSender;
      this.uiDocument = uiDocumentProvider.Entity;

      this.state = streamStateProvider.Entity;
      this.progress = progressProvider.Entity;
    }

    public async Task<string> Send()
    {
      var streamId = state.StreamId;
      var client = state.Client;

      if (!sendSelection.Elements.Any())
        throw new InvalidOperationException(
          "There are zero objects to send. Please use a filter, or set some via selection."
        );

      var commitObject = converter.ConvertToSpeckle(uiDocument.Document) ?? new Collection();
      RevitCommitObjectBuilder commitObjectBuilder = new(CommitCollectionStrategy.ByCollection);

      progress.Report = new ProgressReport();
      progress.Max = sendSelection.Elements.Count;

      var conversionProgressDict = new ConcurrentDictionary<string, int> { ["Conversion"] = 0 };
      var convertedCount = 0;

      await RevitTask.RunAsync(_ =>
      {
        using var _d0 = LogContext.PushProperty("converterName", converter.Name);
        using var _d1 = LogContext.PushProperty("converterAuthor", converter.Author);
        using var _d2 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToSpeckle));

        foreach (var revitElement in sendSelection.Elements)
        {
          if (progress.CancellationToken.IsCancellationRequested)
            break;

          bool isAlreadyConverted = GetOrCreateApplicationObject(
            revitElement,
            converter.Report,
            out ApplicationObject reportObj
          );
          if (isAlreadyConverted)
            continue;

          progress.Report.Log(reportObj);

          //Add context to logger
          using var _d3 = LogContext.PushProperty("elementType", revitElement.GetType());
          using var _d4 = LogContext.PushProperty("elementCategory", revitElement.Category?.Name);

          try
          {
            converter.Report.Log(reportObj); // Log object so converter can access

            Base result = ConvertToSpeckle(revitElement, converter);

            reportObj.Update(
              status: ApplicationObject.State.Created,
              logItem: $"Sent as {ConnectorRevitUtils.SimplifySpeckleType(result.speckle_type)}"
            );
            if (result.applicationId != reportObj.applicationId)
            {
              SpeckleLog.Logger.Information(
                "Conversion result of type {elementType} has a different application Id ({actualId}) to the report object {expectedId}",
                revitElement.GetType(),
                result.applicationId,
                reportObj.applicationId
              );
              result.applicationId = reportObj.applicationId;
            }
            commitObjectBuilder.IncludeObject(result, revitElement);
            convertedCount++;
          }
          catch (ConversionSkippedException ex)
          {
            reportObj.Update(status: ApplicationObject.State.Skipped, logItem: ex.Message);
          }
          catch (Exception ex)
          {
            SpeckleLog.Logger.Error(ex, "Object failed during conversion");
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"{ex.Message}");
          }

          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);

          YieldToUIThread(TimeSpan.FromMilliseconds(1));
        }
      })
      .ConfigureAwait(false);

      progress.Report.Merge(converter.Report);

      progress.CancellationToken.ThrowIfCancellationRequested();

      if (convertedCount == 0)
      {
        throw new SpeckleException("Zero objects converted successfully. Send stopped.");
      }

      commitObjectBuilder.BuildCommitObject(commitObject);

      await speckleObjectSender.Send(streamId, state.BranchName, state.CommitMessage, commitObject, convertedCount)
        .ConfigureAwait(false);

      return speckleObjectSender.CommitId;
    }

    public static bool GetOrCreateApplicationObject(
      Element revitElement,
      ProgressReport report,
      out ApplicationObject reportObj
    )
    {
      if (report.ReportObjects.TryGetValue(revitElement.UniqueId, out var applicationObject))
      {
        reportObj = applicationObject;
        return true;
      }

      string descriptor = ConnectorRevitUtils.ObjectDescriptor(revitElement);
      reportObj = new(revitElement.UniqueId, descriptor) { applicationId = revitElement.UniqueId };
      return false;
    }

    private static void YieldToUIThread(TimeSpan delay)
    {
      using CancellationTokenSource s = new(delay);
      Dispatcher.UIThread.MainLoop(s.Token);
    }

    private static Base ConvertToSpeckle(Element revitElement, ISpeckleConverter converter)
    {
      if (!converter.CanConvertToSpeckle(revitElement))
      {
        string skipMessage = revitElement switch
        {
          RevitLinkInstance => "Enable linked model support from the settings to send this object",
          _ => "Sending this object type is not supported yet"
        };

        throw new ConversionSkippedException(skipMessage, revitElement);
      }

      Base conversionResult = converter.ConvertToSpeckle(revitElement);

      if (conversionResult == null)
        throw new SpeckleException($"Conversion of {revitElement.UniqueId} (ToSpeckle) returned null");

      return conversionResult;
    }
  }
}

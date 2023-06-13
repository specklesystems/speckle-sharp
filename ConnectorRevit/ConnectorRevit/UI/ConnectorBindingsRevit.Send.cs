#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia.Threading;
using ConnectorRevit;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Revit.Async;
using Serilog.Context;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      return await SendStreamTestable(state, new SpeckleObjectServerSender(), progress, Converter.GetType(), CurrentDoc)
        .ConfigureAwait(false);
    }

    public static async Task<string> SendStreamTestable(StreamState state, ISpeckleObjectSender objectSender, ProgressViewModel progress, Type converterType, UIDocument UIDoc) 
    { 
      //make sure to instance a new copy so all values are reset correctly
      var converter = (ISpeckleConverter)Activator.CreateInstance(converterType);
      converter.SetContextDocument(UIDoc.Document);
      converter.Report.ReportObjects.Clear();

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      var streamId = state.StreamId;
      var client = state.Client;

      var selectedObjects = GetSelectionFilterObjects(converter, state.Filter, state.Settings, UIDoc.Document);
      state.SelectedObjectIds = selectedObjects.Select(x => x.UniqueId).ToList();

      if (!selectedObjects.Any())
        throw new InvalidOperationException(
          "There are zero objects to send. Please use a filter, or set some via selection."
        );

      converter.SetContextObjects(
        selectedObjects
          .Select(x => new ApplicationObject(x.UniqueId, x.GetType().ToString()) { applicationId = x.UniqueId })
          .ToList()
      );
      var commitObject = converter.ConvertToSpeckle(UIDoc.Document) ?? new Collection();
      RevitCommitObjectBuilder commitObjectBuilder = new(CommitCollectionStrategy.ByCollection);

      progress.Report = new ProgressReport();
      progress.Max = selectedObjects.Count;

      var conversionProgressDict = new ConcurrentDictionary<string, int> { ["Conversion"] = 0 };
      var convertedCount = 0;

      await RevitTask
        .RunAsync(_ =>
        {
          using var _d0 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToSpeckle));

          foreach (var revitElement in selectedObjects)
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
            using var _d1 = LogContext.PushProperty("elementType", revitElement.GetType());
            using var _d2 = LogContext.PushProperty("elementCategory", revitElement.Category.Name);

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

            YeildToUIThread(TimeSpan.FromMilliseconds(1));
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

      var objectId = await objectSender.Send(client?.Account, streamId, commitObject, progress)
        .ConfigureAwait(true);

      progress.CancellationToken.ThrowIfCancellationRequested();

      //var actualCommit = objectSender.CreateCommitInput(streamId, objectId, state, convertedCount);
      var actualCommit = new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage ?? $"Sent {convertedCount} objects from {ConnectorRevitUtils.RevitAppName}.",
        sourceApplication = ConnectorRevitUtils.RevitAppName,
      };

      if (state.PreviousCommitId != null)
      {
        actualCommit.parents = new List<string>() { state.PreviousCommitId };
      }

      var commitId = await objectSender.CreateCommit(client, actualCommit, progress.CancellationToken)
        .ConfigureAwait(false);

      return commitId;
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

    private static void YeildToUIThread(TimeSpan delay)
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

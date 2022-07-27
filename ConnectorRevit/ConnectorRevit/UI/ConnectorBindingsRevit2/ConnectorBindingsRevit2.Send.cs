using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {
    // used to store the Stream State settings when sending/receiving
    private List<ISetting> CurrentSettings { get; set; }

    public override void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      var filterObjs = GetSelectionFilterObjects(state.Filter);
      foreach (var filterObj in filterObjs)
      {
        var type = filterObj.GetType().ToString();
        var reportObj = new ApplicationObject(filterObj.UniqueId, type);
        if (!Converter.CanConvertToSpeckle(filterObj))
          reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Sending objects of type {type} not supported in Revit");
        else
          reportObj.Update(status: ApplicationObject.State.Created);
        progress.Report.Log(reportObj);
      }
      SelectClientObjects(filterObjs.Select(o => o.UniqueId).ToList());
    }

    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      Converter.SetContextDocument(CurrentDoc.Document);

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      Converter.SetConverterSettings(settings);

      var streamId = state.StreamId;
      var client = state.Client;

      var selectedObjects = GetSelectionFilterObjects(state.Filter);
      state.SelectedObjectIds = selectedObjects.Select(x => x.UniqueId).ToList();

      if (!selectedObjects.Any())
      {
        progress.Report.LogOperationError(new Exception("There are zero objects to send. Please use a filter, or set some via selection."));
        return null;
      }

      Converter.SetContextObjects(selectedObjects.Select(x => new ApplicationObject(x.UniqueId, x.GetType().ToString()) { applicationId = x.UniqueId }).ToList());

      var commitObject = Converter.ConvertToSpeckle(CurrentDoc.Document) ?? new Base();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;

      progress.Max = selectedObjects.Count();
      var convertedCount = 0;
      foreach (var revitElement in selectedObjects)
      {
        var type = revitElement.GetType().ToString();
        var reportObj = new ApplicationObject(revitElement.UniqueId, type) { applicationId = revitElement.UniqueId, Status = ApplicationObject.State.Unknown };
        try
        {
          if (revitElement == null)
            continue;

          if (!Converter.CanConvertToSpeckle(revitElement))
          {
            reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Sending objects of type {type} not supported in Revit");
            progress.Report.Log(reportObj);
            continue;
          }

          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            return null;

          Converter.Report.Log(reportObj); // Log object so converter can access
          var conversionResult = Converter.ConvertToSpeckle(revitElement);

          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);

          convertedCount++;

          //hosted elements will be returned as `null` by the ConvertToSpeckle method 
          //since they are handled when converting their parents
          if (conversionResult == null)
          {
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
            progress.Report.Log(reportObj);
            continue;
          }

          var category = $"@{revitElement.Category.Name}";
          if (commitObject[category] == null)
            commitObject[category] = new List<Base>();

          ((List<Base>)commitObject[category]).Add(conversionResult);

          reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {ConnectorRevitUtils.SimplifySpeckleType(conversionResult.speckle_type)}");
        }
        catch (Exception e)
        {
          reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
        }
        progress.Report.Log(reportObj);
      }

      progress.Report.Merge(Converter.Report);

      if (convertedCount == 0)
      {
        progress.Report.LogOperationError(new Exception("Zero objects converted successfully. Send stopped."));
        return null;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var objectId = await Operations.Send(
        @object: commitObject,
        cancellationToken: progress.CancellationTokenSource.Token,
        transports: transports,
        onProgressAction: dict => progress.Update(dict),
        onErrorAction: (s, e) =>
        {
          progress.Report.LogOperationError(e);
          progress.CancellationTokenSource.Cancel();
        },
        disposeTransports: true
        );

      if (progress.Report.OperationErrorsCount != 0)
        return null;

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      var actualCommit = new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Sent {convertedCount} objects from {ConnectorRevitUtils.RevitAppName}.",
        sourceApplication = ConnectorRevitUtils.RevitAppName,
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }
      string commitId = null;
      try
      {
        commitId = await client.CommitCreate(actualCommit);

        //await state.RefreshStream();
        state.PreviousCommitId = commitId;
      }
      catch (Exception e)
      {
        progress.Report.LogOperationError(e);
      }

      return commitId;
    }

  }
}

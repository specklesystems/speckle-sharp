using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
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
        var descriptor = ConnectorRevitUtils.ObjectDescriptor(filterObj);
        var reportObj = new ApplicationObject(filterObj.UniqueId, descriptor);
        if (!converter.CanConvertToSpeckle(filterObj))
          reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Sending this object type is not supported in Revit");
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
      //make sure to instance a new copy so all values are reset correctly
      var converter = (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
      converter.SetContextDocument(CurrentDoc.Document);
      converter.Report.ReportObjects.Clear();

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      var streamId = state.StreamId;
      var client = state.Client;

      var selectedObjects = GetSelectionFilterObjects(state.Filter);
      state.SelectedObjectIds = selectedObjects.Select(x => x.UniqueId).ToList();

      if (!selectedObjects.Any())
      {
        progress.Report.LogOperationError(new Exception("There are zero objects to send. Please use a filter, or set some via selection."));
        return null;
      }

      converter.SetContextObjects(selectedObjects.Select(x => new ApplicationObject(x.UniqueId, x.GetType().ToString()) { applicationId = x.UniqueId }).ToList());
      var commitObject = converter.ConvertToSpeckle(CurrentDoc.Document) ?? new Base();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;

      progress.Max = selectedObjects.Count();
      var convertedCount = 0;
      foreach (var revitElement in selectedObjects)
      {
        var descriptor = ConnectorRevitUtils.ObjectDescriptor(revitElement);
        // get the report object
        // for hosted elements, they may have already been converted and added to the converter report
        bool alreadyConverted = converter.Report.GetReportObject(revitElement.UniqueId, out int index);
        var reportObj = alreadyConverted ?
          converter.Report.ReportObjects[index] :
          new ApplicationObject(revitElement.UniqueId, descriptor) { applicationId = revitElement.UniqueId };
        if (alreadyConverted)
        {
          progress.Report.Log(reportObj);
          continue;
        }
        try
        {
          if (revitElement == null)
            continue;

          if (!converter.CanConvertToSpeckle(revitElement))
          {
            reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Sending this object type is not supported in Revit");
            progress.Report.Log(reportObj);
            continue;
          }

          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            return null;

          converter.Report.Log(reportObj); // Log object so converter can access
          var conversionResult = converter.ConvertToSpeckle(revitElement);

          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);

          convertedCount++;

          if (conversionResult == null)
          {
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
            progress.Report.Log(reportObj);
            continue;
          }

          //is an element type, nest it under Types instead
          if (typeof(ElementType).IsAssignableFrom(revitElement.GetType()))
          {
            var category = $"@{revitElement.Category.Name}";

            if (commitObject["Types"] == null)
              commitObject["Types"] = new Base();

            if ((commitObject["Types"] as Base)[category] == null)
              (commitObject["Types"] as Base)[category] = new List<Base>();

            ((List<Base>)((commitObject["Types"] as Base)[category])).Add(conversionResult);
          }
          else
          {
            var category = $"@{revitElement.Category.Name}";
            if (commitObject[category] == null)
              commitObject[category] = new List<Base>();

            ((List<Base>)commitObject[category]).Add(conversionResult);
          }


          reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {ConnectorRevitUtils.SimplifySpeckleType(conversionResult.speckle_type)}");
        }
        catch (Exception e)
        {
          reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
        }
        progress.Report.Log(reportObj);
      }

      progress.Report.Merge(converter.Report);

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

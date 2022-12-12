using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Avalonia.Threading;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Revit.Async;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    // used to store the Stream State settings when sending/receiving
    private List<ISetting> CurrentSettings { get; set; }

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

      var cancelSend = await RevitTask.RunAsync(app =>
      {
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
              return true;

            converter.Report.Log(reportObj); // Log object so converter can access

            var conversionResult = converter.ConvertToSpeckle(revitElement);

            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);

            var s = new CancellationTokenSource();
            DispatcherTimer.RunOnce(() => s.Cancel(), TimeSpan.FromMilliseconds(1));
            Dispatcher.UIThread.MainLoop(s.Token);

            convertedCount++;

            if (conversionResult == null)
            {
              reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
              progress.Report.Log(reportObj);
              continue;
            }

            // here we are checking to see if we're receiving an object that has a host
            // but the host doesn't know that it is a host
            if (conversionResult["speckleHost"] is Base host && host["category"] is string catName)
            {
              commitObject[$"@{catName}"] ??= new List<Base>();
              if (commitObject[$"@{catName}"] is List<Base> objs)
              {
                var hostIndex = objs.FindIndex(obj => obj.applicationId == host.applicationId);
                // if the "host" is present, then it has already been converted and we need to 
                // attach the current, dependent, elements as a hosted element
                if (hostIndex != -1)
                {
                  objs[hostIndex]["elements"] ??= new List<Base>();
                  ((List<Base>)objs[hostIndex]["elements"]).Add(conversionResult);
                }
                // if host is not present, then it hasn't been converted yet
                // create a placeholder that will be overridden later, but that will contain the hosted element
                else
                {
                  var newBase = new Base() { applicationId = host.applicationId };
                  newBase["elements"] = new List<Base>() { conversionResult };
                  objs.Add(newBase);
                }

                // remove the speckleHost element that we added
                conversionResult["speckleHost"] = null;

                reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Attached as hosted element to {host.applicationId}");
              }
            }
            //is an element type, nest it under Types instead
            else if (typeof(ElementType).IsAssignableFrom(revitElement.GetType()))
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
              var category = conversionResult.GetType().Name == "Network" ?
                "@Networks" :
                $"@{revitElement.Category.Name}";

              commitObject[category] ??= new List<Base>();

              if (commitObject[category] is List<Base> objs)
              {
                var hostIndex = objs.FindIndex(obj => obj.applicationId == conversionResult.applicationId);
              
                // here we are checking to see if we're converting a host that doesn't know it is a host
                // and if dependent elements of that host have already been converted
                if (hostIndex != -1 && objs[hostIndex]["elements"] is List<Base> elements)
                {
                  objs.RemoveAt(hostIndex);
                  if (conversionResult["elements"] is List<Base> els)
                    els.AddRange(elements);
                  else
                    conversionResult["elements"] = elements;
                }
                objs.Add(conversionResult);
              }
            }

            reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {ConnectorRevitUtils.SimplifySpeckleType(conversionResult.speckle_type)}");
          }
          catch (Exception e)
          {
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
          }
          progress.Report.Log(reportObj);
        }
        return false;
      });

      if (cancelSend)
        return null;

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

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {


    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task SendStream(StreamState state, ProgressViewModel progress)
    {

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);

      var streamId = state.StreamId;
      var client = state.Client;

      var selectedObjects = GetSelectionFilterObjects(state.Filter);
      state.SelectedObjectIds = selectedObjects.Select(x => x.UniqueId).ToList();



      if (!selectedObjects.Any())
      {
        progress.Report.LogOperationError(new Exception("There are zero objects to send. Please use a filter, or set some via selection."));
        return;
      }

      converter.SetContextObjects(selectedObjects.Select(x => new ApplicationPlaceholderObject { applicationId = x.UniqueId }).ToList());

      var commitObject = new Base();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;

      progress.Max = selectedObjects.Count();
      var convertedCount = 0;

      var placeholders = new List<Base>();
      foreach (var revitElement in selectedObjects)
      {
        try
        {
          if (revitElement == null)
          {
            continue;
          }

          if (!converter.CanConvertToSpeckle(revitElement))
          {
            progress.Report.Log($"Skipped not supported type: {revitElement.GetType()}, name {revitElement.Name}");
            continue;
          }

          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
          {
            return;
          }

          var conversionResult = converter.ConvertToSpeckle(revitElement);

          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);


          placeholders.Add(new ApplicationPlaceholderObject { applicationId = revitElement.UniqueId, ApplicationGeneratedId = revitElement.UniqueId });

          convertedCount++;

          //hosted elements will be returned as `null` by the ConvertToSpeckle method 
          //since they are handled when converting their parents
          if (conversionResult != null)
          {
            var category = $"@{revitElement.Category.Name}";
            if (commitObject[category] == null)
            {
              commitObject[category] = new List<Base>();
            }
             ((List<Base>)commitObject[category]).Add(conversionResult);
          }

        }
        catch (Exception e)
        {
          progress.Report.LogConversionError(e);
        }
      }

      progress.Report.Merge(converter.Report);

      if (convertedCount == 0)
      {
        progress.Report.LogConversionError(new Exception("Zero objects converted successfully. Send stopped."));
        return;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return;

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
        return;

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return;

      var actualCommit = new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Sent {convertedCount} objects from {ConnectorRevitUtils.RevitAppName}.",
        sourceApplication = ConnectorRevitUtils.RevitAppName,
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);

        //await state.RefreshStream();
        state.PreviousCommitId = commitId;
      }
      catch (Exception e)
      {
        progress.Report.LogOperationError(e);
      }

      //return state;
    }

  }
}

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task<StreamState> SendStream(StreamState state)
    {
      ConversionErrors.Clear();
      OperationErrors.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);

      var streamId = state.Stream.id;
      var client = state.Client;

      var selectedObjects = new List<Element>();

      if (state.Filter != null)
      {
        selectedObjects = GetSelectionFilterObjects(state.Filter, converter);
        state.SelectedObjectIds = selectedObjects.Select(x => x.UniqueId).ToList();
      }
      else //selection was by cursor
      {
        // TODO: update state by removing any deleted or null object ids
        selectedObjects = state.SelectedObjectIds.Select(x => CurrentDoc.Document.GetElement(x)).Where(x => x != null).ToList();
      }

      if (!selectedObjects.Any())
      {
        state.Errors.Add(new Exception("There are zero objects to send. Please use a filter, or set some via selection."));
        return state;
      }

      converter.SetContextObjects(selectedObjects.Select(x => new ApplicationObject(x.UniqueId, x.GetType().ToString()) { applicationId = x.UniqueId }).ToList());

      var commitObject = new Base();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = selectedObjects.Count());
      var convertedCount = 0;

      var placeholders = new List<Base>();
      foreach (var revitElement in selectedObjects)
      {
        try
        {
          if (revitElement == null)
            continue;

          if (!converter.CanConvertToSpeckle(revitElement))
          {
            state.Errors.Add(new Exception($"Skipping not supported type: {revitElement.GetType()}, name {revitElement.Name}"));
            continue;
          }

          var conversionResult = converter.ConvertToSpeckle(revitElement);

          conversionProgressDict["Conversion"]++;
          UpdateProgress(conversionProgressDict, state.Progress);

          placeholders.Add(new ApplicationObject(revitElement.UniqueId, revitElement.GetType().ToString()) { applicationId = revitElement.UniqueId });

          convertedCount++;

          //hosted elements will be returned as `null` by the ConvertToSpeckle method 
          //since they are handled when converting their parents
          if (conversionResult != null)
          {
            var category = $"@{revitElement.Category.Name}";
            if (commitObject[category] == null)
              commitObject[category] = new List<Base>();

            ((List<Base>)commitObject[category]).Add(conversionResult);
          }
        }
        catch (Exception e)
        {
          state.Errors.Add(e);
        }
      }

      if (converter.Report.ConversionErrorsCount != 0)
      {
        // TODO: Get rid of the custom Error class. It's not needed.
        ConversionErrors.AddRange(converter.Report.ConversionErrors);
        state.Errors.AddRange(converter.Report.ConversionErrors);
      }

      if (convertedCount == 0)
      {
        Globals.Notify("Zero objects converted successfully. Send stopped.");
        return state;
      }

      Execute.PostToUIThread(() => state.Progress.Maximum = (int)commitObject.GetTotalChildrenCount());

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
        return state;

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var objectId = await Operations.Send(
        @object: commitObject,
        cancellationToken: state.CancellationTokenSource.Token,
        transports: transports,
        onProgressAction: dict => UpdateProgress(dict, state.Progress),
        onErrorAction: (s, e) =>
        {
          OperationErrors.Add(e); // TODO!
          state.Errors.Add(e);
          state.CancellationTokenSource.Cancel();
        },
        disposeTransports: true
        );

      if (OperationErrors.Count != 0)
      {
        Globals.Notify("Failed to send.");
        state.Errors.AddRange(OperationErrors);
        return state;
      }

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      var actualCommit = new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.Branch.name,
        message = state.CommitMessage != null ? state.CommitMessage : $"Sent {convertedCount} objects from {ConnectorRevitUtils.RevitAppName}.",
        sourceApplication = ConnectorRevitUtils.RevitAppName,
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);

        await state.RefreshStream();
        state.PreviousCommitId = commitId;

        WriteStateToFile();
        RaiseNotification($"{convertedCount} objects sent to Speckle 🚀");
      }
      catch (Exception e)
      {
        state.Errors.Add(e);
        Globals.Notify($"Failed to create commit.\n{e.Message}");
      }

      return state;
    }

  }
}

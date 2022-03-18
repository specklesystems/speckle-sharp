using System;
using System.Collections.Concurrent;
using Speckle.Core.Api;
using SCT = Speckle.Core.Transports;
using Stylet;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.ConnectorTeklaStructures.Util;
using System.Linq;
using DesktopUI2.ViewModels;
using Tekla.Structures.Model;



namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {
    #region sending

    public override async System.Threading.Tasks.Task SendStream(StreamState state, ProgressViewModel progress)
    {
      //throw new NotImplementedException();
      var kit = KitManager.GetDefaultKit();
      //var converter = new ConverterTeklaStructures();
      var converter = kit.LoadConverter(ConnectorTeklaStructuresUtils.TeklaStructuresAppName);
      converter.SetContextDocument(Model);
      Exceptions.Clear();

      var commitObj = new Base();
      int objCount = 0;

      var selectedObjects = new List<ModelObject>();

      if (state.Filter != null)
      {
        selectedObjects = GetSelectionFilterObjects(state.Filter);
        state.SelectedObjectIds = selectedObjects.Select(x => x.Identifier.GUID.ToString()).ToList();
      }

      var totalObjectCount = state.SelectedObjectIds.Count();

      if (totalObjectCount == 0)
      {
        progress.Report.LogOperationError(new SpeckleException("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.", false));
        return;
      }

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      progress.Update(conversionProgressDict);


      //if( commitObj["@Stories"] == null)
      //{
      //    commitObj["@Stories"] = converter.ConvertToSpeckle(("Stories", "TeklaStructures"));
      //}

      foreach (ModelObject obj in selectedObjects)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return;
        }

        Base converted = null;
        string containerName = string.Empty;


        //var selectedObjectType = ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames
        //    .Where(pair => pair.Key == applicationId)
        //    .Select(pair => pair.Value.Item1).FirstOrDefault();

        if (!converter.CanConvertToSpeckle(obj))
        {
          progress.Report.Log($"Skipped not supported type:  ${obj.GetType()} are not supported");
          continue;
        }

        Tracker.TrackPageview(Tracker.CONVERT_TOSPECKLE);

        //var typeAndName = ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames
        //    .Where(pair => pair.Key == applicationId)
        //    .Select(pair => pair.Value).FirstOrDefault();

        converted = converter.ConvertToSpeckle(obj);

        if (converted == null)
        {
          var exception = new Exception($"Failed to convert object ${obj.Identifier.GUID} of type ${obj.GetType()}.");
          progress.Report.LogConversionError(exception);
          continue;
        }


        if (converted != null)
        {
          if (commitObj["@Base"] == null)
          {
            commitObj["@Base"] = new List<Base>();
          }
                     ((List<Base>)commitObj["@Base"]).Add(converted);
        }

        objCount++;
        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      }

      progress.Report.Merge(converter.Report);

      if (objCount == 0)
      {
        progress.Report.LogOperationError(new SpeckleException("Zero objects converted successfully. Send stopped.", false));
        return;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return;
      }

      var streamId = state.StreamId;
      var client = state.Client;

      var transports = new List<SCT.ITransport>() { new SCT.ServerTransport(client.Account, streamId) };

      var objectId = await Operations.Send(
          @object: commitObj,
          cancellationToken: progress.CancellationTokenSource.Token,
          transports: transports,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: (Action<string, Exception>)((s, e) =>
          {
            progress.Report.LogOperationError(e);
            progress.CancellationTokenSource.Cancel();
          }),
          disposeTransports: true
          );


      if (progress.Report.OperationErrorsCount != 0)
      {
        //RaiseNotification($"Failed to send: \n {Exceptions.Last().Message}");
        return;
      }

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {objCount} elements from TeklaStructures.",
        sourceApplication = ConnectorTeklaStructuresUtils.TeklaStructuresAppName
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);

        //await state.RefreshStream();
        state.PreviousCommitId = commitId;

        //PersistAndUpdateStreamInFile(state);
        //RaiseNotification($"{objCount} objects sent to {state.Stream.name}. 🚀");
      }
      catch (Exception e)
      {
        //Globals.Notify($"Failed to create commit.\n{e.Message}");
        progress.Report.LogOperationError(e);
      }

      //return state;
    }

    #endregion
  }
}

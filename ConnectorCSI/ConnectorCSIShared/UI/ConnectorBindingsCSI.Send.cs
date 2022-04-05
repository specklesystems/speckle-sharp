using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorCSI.Util;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SCT = Speckle.Core.Transports;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings

  {
    #region sending

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      //throw new NotImplementedException();
      var kit = KitManager.GetDefaultKit();
      //var converter = new ConverterCSI();
      var appName = GetHostAppVersion(Model);
      var converter = kit.LoadConverter(appName);
      converter.SetContextDocument(Model);
      Exceptions.Clear();

      var commitObj = new Base();
      int objCount = 0;

      if (state.Filter != null)
      {
        state.SelectedObjectIds = GetSelectionFilterObjects(state.Filter);
      }

      var totalObjectCount = state.SelectedObjectIds.Count();

      if (totalObjectCount == 0)
      {
        progress.Report.LogOperationError(new SpeckleException("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.", false));
        return null;
      }

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      progress.Update(conversionProgressDict);


      //if( commitObj["@Stories"] == null)
      //{
      //    commitObj["@Stories"] = converter.ConvertToSpeckle(("Stories", "CSI"));
      //}

      foreach (var applicationId in state.SelectedObjectIds)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return null;
        }

        Base converted = null;
        string containerName = string.Empty;


        var selectedObjectType = ConnectorCSIUtils.ObjectIDsTypesAndNames
            .Where(pair => pair.Key == applicationId)
            .Select(pair => pair.Value.Item1).FirstOrDefault();

        if (!converter.CanConvertToSpeckle(selectedObjectType))
        {
          progress.Report.Log($"Skipped not supported type:  ${selectedObjectType} are not supported");
          continue;
        }


        var typeAndName = ConnectorCSIUtils.ObjectIDsTypesAndNames
            .Where(pair => pair.Key == applicationId)
            .Select(pair => pair.Value).FirstOrDefault();

        converted = converter.ConvertToSpeckle(typeAndName);

        if (converted == null)
        {
          var exception = new Exception($"Failed to convert object ${applicationId} of type ${selectedObjectType}.");
          progress.Report.LogConversionError(exception);
          continue;
        }


        //if (converted != null)
        //{
        //    if (commitObj[selectedObjectType] == null)
        //    {
        //        commitObj[selectedObjectType] = new List<Base>();
        //    }
        //             ((List<Base>)commitObj[selectedObjectType]).Add(converted);
        //}

        //objCount++;
        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      }

      Base ElementCount = converter.ConvertToSpeckle(("ElementsCount", "CSI"));
      if (ElementCount.applicationId != null)
      {
        objCount = Convert.ToInt32(ElementCount.applicationId);
      }
      else
      {
        objCount = 0;
      }


      if (commitObj["@Model"] == null)
      {
        commitObj["@Model"] = converter.ConvertToSpeckle(("Model", "CSI"));
      }

      if (commitObj["AnalysisResults"] == null)
      {
        commitObj["AnalysisResults"] = converter.ConvertToSpeckle(("AnalysisResults", "CSI"));
      }

      progress.Report.Merge(converter.Report);

      if (objCount == 0)
      {
        progress.Report.LogOperationError(new SpeckleException("Zero objects converted successfully. Send stopped.", false));
        return null;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
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
        return null;
      }

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {objCount} elements from CSI.",
        sourceApplication = GetHostAppVersion(Model)
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);

        //await state.RefreshStream();
        state.PreviousCommitId = commitId;
        return commitId;

        //PersistAndUpdateStreamInFile(state);
        //RaiseNotification($"{objCount} objects sent to {state.Stream.name}. 🚀");
      }
      catch (Exception e)
      {
        //Globals.Notify($"Failed to create commit.\n{e.Message}");
        progress.Report.LogOperationError(e);
      }
      return null;
      //return state;
    }

    #endregion
  }
}
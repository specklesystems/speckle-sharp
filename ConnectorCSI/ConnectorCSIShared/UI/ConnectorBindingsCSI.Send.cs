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
    public override bool CanPreviewSend => false;
    public override void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      // TODO!
    }

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      //var converter = new ConverterCSI();
      var appName = GetHostAppVersion(Model);
      var converter = kit.LoadConverter(appName);

      // set converter settings as tuples (setting slug, setting selection)
      // for csi, these must go before the SetContextDocument method.
      var settings = new Dictionary<string, string>();
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      settings.Add("operation", "send");
      converter.SetConverterSettings(settings);

      converter.SetContextDocument(Model);
      converter.SetPreviousContextObjects(state.ReceivedObjects);
      Exceptions.Clear();

      int objCount = 0;

      if (state.Filter != null)
        state.SelectedObjectIds = GetSelectionFilterObjects(state.Filter);

      var totalObjectCount = state.SelectedObjectIds.Count();

      if (totalObjectCount == 0)
      {
        throw new InvalidOperationException( "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
      }

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      progress.Max = totalObjectCount;
      conversionProgressDict["Conversion"] = 0;
      progress.Update(conversionProgressDict);

      BuildSendCommitObj(converter, state.SelectedObjectIds, ref progress, ref conversionProgressDict);

      var commitObj = GetCommitObj(converter, progress, conversionProgressDict);

      return await SendCommitObj(state, progress, commitObj, conversionProgressDict);
    }

    public void BuildSendCommitObj(ISpeckleConverter converter, List<string> selectedObjIds, ref ProgressViewModel progress, ref ConcurrentDictionary<string, int> conversionProgressDict)
    {
      foreach (var applicationId in selectedObjIds)
      {
        progress.CancellationToken.ThrowIfCancellationRequested();

        Base converted = null;
        string containerName = string.Empty;


        var selectedObjectType = ConnectorCSIUtils.ObjectIDsTypesAndNames
            .Where(pair => pair.Key == applicationId)
            .Select(pair => pair.Value.Item1).FirstOrDefault();

        var reportObj = new ApplicationObject(applicationId, selectedObjectType) { applicationId = applicationId };

        if (!converter.CanConvertToSpeckle(selectedObjectType))
        {
          progress.Report.Log($"Skipped not supported type:  ${selectedObjectType} are not supported");
          continue;
        }

        var typeAndName = ConnectorCSIUtils.ObjectIDsTypesAndNames
            .Where(pair => pair.Key == applicationId)
            .Select(pair => pair.Value).FirstOrDefault();

        try
        {
          converted = converter.ConvertToSpeckle(typeAndName);
        }
        catch (Exception ex)
        {
          reportObj.Update(status: ApplicationObject.State.Failed, logItem: ex.Message);
          progress.Report.Log(reportObj);
          continue;
        }

        if (converted == null)
        {
          reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
          progress.Report.Log(reportObj);
          continue;
        }

        reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {ConnectorCSIUtils.SimplifySpeckleType(converted.speckle_type)}");
        progress.Report.Log(reportObj);

        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      }
    }

    public Base GetCommitObj(ISpeckleConverter converter, ProgressViewModel progress, ConcurrentDictionary<string, int> conversionProgressDict)
    {
      var commitObj = new Base();
      var reportObj = new ApplicationObject("model", "ModelInfo");
      if (commitObj["@Model"] == null)
      {
        try
        {
          commitObj["@Model"] = converter.ConvertToSpeckle(("Model", "CSI"));
          reportObj.Update(status: ApplicationObject.State.Created);
        }
        catch (Exception ex)
        {
          reportObj.Update(status: ApplicationObject.State.Failed, logItem: ex.Message);
        }
        progress.Report.Log(reportObj);
      }

      reportObj = new ApplicationObject("results", "AnalysisResults");
      if (commitObj["AnalysisResults"] == null)
      {
        try
        {
          commitObj["AnalysisResults"] = converter.ConvertToSpeckle(("AnalysisResults", "CSI"));
          reportObj.Update(status: ApplicationObject.State.Created);
        }
        catch (Exception ex)
        {
          reportObj.Update(status: ApplicationObject.State.Failed, logItem: ex.Message);
        }
        progress.Report.Log(reportObj);
      }

      progress.Report.Merge(converter.Report);

      if (conversionProgressDict["Conversion"] == 0)
      {
        throw new SpeckleException("Zero objects converted successfully. Send stopped.");
      }

      progress.CancellationToken.ThrowIfCancellationRequested();

      return commitObj;
    }

    public async Task<string> SendCommitObj(StreamState state, ProgressViewModel progress, Base commitObj, ConcurrentDictionary<string, int> conversionProgressDict, string branchName = null)
    {
      var streamId = state.StreamId;
      var client = state.Client;

      var transports = new List<SCT.ITransport>() { new SCT.ServerTransport(client.Account, streamId) };
      progress.Max = conversionProgressDict["Conversion"];

      var objectId = await Operations.Send(
          @object: commitObj,
          cancellationToken: progress.CancellationToken,
          transports: transports,
          onProgressAction: dict =>
          {
            progress.Update(dict);
          },
          onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
          disposeTransports: true
          );
      
      
      if (branchName != null)
      {
        var branchesSplit = state.BranchName.Split('/');
        branchesSplit[branchesSplit.Count() - 1] = branchName;
        branchName = string.Join("", branchesSplit);

        var branchInput = new BranchCreateInput() { streamId = streamId, name = branchName, description = "This branch holds the comprehensive reports generated by Speckle"};
        var branch = await client.BranchGet(streamId, branchName);
        if (branch == null)
          await client.BranchCreate(branchInput);
      }
      else
        branchName = state.BranchName;

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = branchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {conversionProgressDict["Conversion"]} elements from CSI.",
        sourceApplication = GetHostAppVersion(Model)
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      return await ConnectorHelpers.CreateCommit(client, actualCommit, progress.CancellationToken);
      
    }
  }
}

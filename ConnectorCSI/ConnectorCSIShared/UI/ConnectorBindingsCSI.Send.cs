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
using Serilog.Context;
using SCT = Speckle.Core.Transports;

namespace Speckle.ConnectorCSI.UI;

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
    {
      settings.Add(setting.Slug, setting.Selection);
    }

    settings.Add("operation", "send");
    converter.SetConverterSettings(settings);

    converter.SetContextDocument(Model);
    converter.SetPreviousContextObjects(state.ReceivedObjects);
    Exceptions.Clear();

    int objCount = 0;

    if (state.Filter != null)
    {
      state.SelectedObjectIds = GetSelectionFilterObjects(state.Filter);
    }

    var totalObjectCount = state.SelectedObjectIds.Count;

    if (totalObjectCount == 0)
    {
      throw new InvalidOperationException(
        "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."
      );
    }

    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    progress.Max = totalObjectCount;
    conversionProgressDict["Conversion"] = 0;
    progress.Update(conversionProgressDict);

    using var d0 = LogContext.PushProperty("converterName", converter.Name);
    using var d1 = LogContext.PushProperty("converterAuthor", converter.Author);
    using var d2 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToSpeckle));
    using var d3 = LogContext.PushProperty("converterSettings", settings);

    BuildSendCommitObj(converter, state.SelectedObjectIds, ref progress, ref conversionProgressDict);

    var commitObj = GetCommitObj(converter, progress, conversionProgressDict);

    return await SendCommitObj(state, progress, commitObj, conversionProgressDict);
  }

  public void BuildSendCommitObj(
    ISpeckleConverter converter,
    List<string> selectedObjIds,
    ref ProgressViewModel progress,
    ref ConcurrentDictionary<string, int> conversionProgressDict
  )
  {
    foreach (var applicationId in selectedObjIds)
    {
      progress.CancellationToken.ThrowIfCancellationRequested();

      Base converted = null;
      string containerName = string.Empty;

      var selectedObjectType = ConnectorCSIUtils.ObjectIDsTypesAndNames
        .Where(pair => pair.Key == applicationId)
        .Select(pair => pair.Value.Item1)
        .FirstOrDefault();

      var reportObj = new ApplicationObject(applicationId, selectedObjectType) { applicationId = applicationId };

      if (!converter.CanConvertToSpeckle(selectedObjectType))
      {
        progress.Report.Log($"Skipped not supported type:  ${selectedObjectType} are not supported");
        continue;
      }

      var typeAndName = ConnectorCSIUtils.ObjectIDsTypesAndNames
        .Where(pair => pair.Key == applicationId)
        .Select(pair => pair.Value)
        .FirstOrDefault();

      using var _0 = LogContext.PushProperty("fromType", typeAndName.typeName);

      try
      {
        converted = converter.ConvertToSpeckle(typeAndName);
        if (converted == null)
        {
          throw new ConversionException("Conversion Returned Null");
        }

        reportObj.Update(
          status: ApplicationObject.State.Created,
          logItem: $"Sent as {ConnectorCSIUtils.SimplifySpeckleType(converted.speckle_type)}"
        );
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        ConnectorHelpers.LogConversionException(ex);

        var failureStatus = ConnectorHelpers.GetAppObjectFailureState(ex);
        reportObj.Update(status: failureStatus, logItem: ex.Message);
      }

      progress.Report.Log(reportObj);

      conversionProgressDict["Conversion"]++;
      progress.Update(conversionProgressDict);
    }
  }

  public Base GetCommitObj(
    ISpeckleConverter converter,
    ProgressViewModel progress,
    ConcurrentDictionary<string, int> conversionProgressDict
  )
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
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.Error(ex, "Error when attempting to retreive commit object");
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
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.Error(ex, "Error when attempting to retreive analysis results");
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

  public async Task<string> SendCommitObj(
    StreamState state,
    ProgressViewModel progress,
    Base commitObj,
    ConcurrentDictionary<string, int> conversionProgressDict,
    string branchName = null
  )
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

      var branchInput = new BranchCreateInput()
      {
        streamId = streamId,
        name = branchName,
        description = "This branch holds the comprehensive reports generated by Speckle"
      };
      var branch = await client.BranchGet(streamId, branchName);
      if (branch == null)
      {
        await client.BranchCreate(branchInput);
      }
    }
    else
    {
      branchName = state.BranchName;
    }

    var actualCommit = new CommitCreateInput
    {
      streamId = streamId,
      objectId = objectId,
      branchName = branchName,
      message =
        state.CommitMessage != null
          ? state.CommitMessage
          : $"Pushed {conversionProgressDict["Conversion"]} elements from CSI.",
      sourceApplication = GetHostAppVersion(Model)
    };

    if (state.PreviousCommitId != null)
    {
      actualCommit.parents = new List<string>() { state.PreviousCommitId };
    }

    return await ConnectorHelpers.CreateCommit(client, actualCommit, progress.CancellationToken);
  }
}

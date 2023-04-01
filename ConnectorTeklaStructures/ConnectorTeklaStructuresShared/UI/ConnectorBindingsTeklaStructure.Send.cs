using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorTeklaStructures.Util;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Model;
using SCT = Speckle.Core.Transports;



namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {
    #region sending

    private List<ISetting> CurrentSettings { get; set; }

    public override bool CanPreviewSend => false;
    public override void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      return;
    }

    public override async System.Threading.Tasks.Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorTeklaStructuresUtils.TeklaStructuresAppName);
      converter.SetContextDocument(Model);
      Exceptions.Clear();

      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

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
        throw new InvalidOperationException(
          "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
      }

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      progress.Max = totalObjectCount;
      conversionProgressDict["Conversion"] = 0;
      progress.Update(conversionProgressDict);





      foreach (ModelObject obj in selectedObjects)
      {
        if (progress.CancellationToken.IsCancellationRequested)
        {
          return null;
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

        //var typeAndName = ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames
        //    .Where(pair => pair.Key == applicationId)
        //    .Select(pair => pair.Value).FirstOrDefault();

        try
        {
          converted = converter.ConvertToSpeckle(obj);
        }
        catch (Exception ex)
        {
          //TODO: log
        }

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
        throw new SpeckleException("Zero objects converted successfully. Send stopped.");
      }

      progress.CancellationToken.ThrowIfCancellationRequested();

      var streamId = state.StreamId;
      var client = state.Client;

      var transports = new List<SCT.ITransport>() { new SCT.ServerTransport(client.Account, streamId) };
      progress.Max = totalObjectCount;
      var objectId = await Operations.Send(
          @object: commitObj,
          cancellationToken: progress.CancellationToken,
          transports: transports,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
          disposeTransports: true
          );
      
      progress.CancellationToken.ThrowIfCancellationRequested();

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {objCount} elements from TeklaStructures.",
        sourceApplication = ConnectorTeklaStructuresUtils.TeklaStructuresAppName
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      var commitId = await ConnectorHelpers.CreateCommit(progress.CancellationToken, client, actualCommit);
      return commitId;
    }

    #endregion
  }
}

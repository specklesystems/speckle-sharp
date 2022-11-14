using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Collections;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using static DesktopUI2.ViewModels.MappingViewModel;
using ApplicationObject = Speckle.Core.Models.ApplicationObject;
using Avalonia.Threading;
using Autodesk.Revit.DB.DirectContext3D;
using Revit.Async;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {
    public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
    public override bool CanPreviewReceive => true;
    private string SelectedReceiveCommit { get; set; }
    public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
 

      // first check if commit is the same and preview objects have already been generated
      Commit commit = await GetCommitFromState(state, progress);
      progress.Report = new ProgressReport();

      if (commit.id != SelectedReceiveCommit)
      {
        // check for converter 
        var converter = KitManager.GetDefaultKit().LoadConverter(ConnectorRevitUtils.RevitAppName);
        if (converter == null)
        {
          progress.Report.LogOperationError(new SpeckleException("Could not find any Kit!"));
          return null;
        }
        converter.SetContextDocument(CurrentDoc.Document);

        var settings = new Dictionary<string, string>();
        CurrentSettings = state.Settings;
        foreach (var setting in state.Settings)
          settings.Add(setting.Slug, setting.Selection);

        settings["preview"] = "true";
        converter.SetConverterSettings(settings);

        var commitObject = await GetCommit(commit, state, progress);
        if (commitObject == null)
        {
          progress.Report.LogOperationError(new Exception($"Could not retrieve commit {commit.id} from server"));
          progress.CancellationTokenSource.Cancel();
        }

        Preview.Clear();
        StoredObjects.Clear();

        Preview = FlattenCommitObject(commitObject, converter);
        foreach (var previewObj in Preview)
          progress.Report.Log(previewObj);

        List<ApplicationObject> applicationObjects = null;
        await RevitTask.RunAsync(app =>
        {
          using (var t = new Transaction(CurrentDoc.Document, $"Baking stream {state.StreamId}"))
          {
            //t.Start();
            applicationObjects = ConvertReceivedObjects(converter, progress);
            //t.Commit();
          }

          AddMultipleRevitElementServers(applicationObjects);
        });
      }
      else // just generate the log
      {
        foreach (var previewObj in Preview)
          progress.Report.Log(previewObj);
      }
      return null;

        //var converter = KitManager.GetDefaultKit().LoadConverter(ConnectorRevitUtils.RevitAppName);
        //converter.SetContextDocument(CurrentDoc.Document);
        //var settings = new Dictionary<string, string>();
        //settings["preview"] = "true";
        //converter.SetConverterSettings(settings);

        //var x = converter.ConvertToNative(new Base()) as ApplicationObject;

        //AddMultipleRevitElementServers(new List<ApplicationObject>() { x });

        //return null;
    }
    #region move to core?
    // gets the state commit
    private async Task<Commit> GetCommitFromState(StreamState state, ProgressViewModel progress)
    {
      Commit commit = null;
      if (state.CommitId == "latest") //if "latest", always make sure we get the latest commit
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        commit = res.commits.items.FirstOrDefault();
      }
      else
      {
        var res = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
        commit = res;
      }
      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;
      return commit;
    }
    private async Task<Base> GetCommit(Commit commit, StreamState state, ProgressViewModel progress)
    {
      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      var commitObject = await Operations.Receive(
        commit.referencedObject,
        progress.CancellationTokenSource.Token,
        transport,
        onProgressAction: dict => progress.Update(dict),
        onErrorAction: (s, e) =>
        {
          progress.Report.LogOperationError(e);
          progress.CancellationTokenSource.Cancel();
        },
        onTotalChildrenCountKnown: (c) => progress.Max = c,
        disposeTransports: true
        );

      if (progress.Report.OperationErrorsCount != 0)
        return null;

      return commitObject;
    }
    #endregion
    public void AddMultipleRevitElementServers(List<ApplicationObject> applicationObjects)
    {
      ExternalService directContext3DService = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
      MultiServerService msDirectContext3DService = directContext3DService as MultiServerService;
      IList<Guid> serverIds = msDirectContext3DService.GetActiveServerIds();

      foreach (var appObj in applicationObjects)
      {
        if (!(appObj.Converted.FirstOrDefault() is IExternalServer server))
          continue;

        directContext3DService.AddServer(server);
        serverIds.Add(server.GetServerId());
      }

      msDirectContext3DService.SetActiveServers(serverIds);

      //m_documents.Add(uidoc.Document);
      CurrentDoc.UpdateAllOpenViews();
    }
  }
}

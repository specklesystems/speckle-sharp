using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using RevitSharedResources.Interfaces;
using RevitSharedResources.Models;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using ApplicationObject = Speckle.Core.Models.ApplicationObject;

namespace Speckle.ConnectorRevit.UI;

public partial class ConnectorBindingsRevit
{
  public override bool CanPreviewReceive => false;
  private string SelectedReceiveCommit { get; set; }
  List<IDirectContext3DServer> m_servers = new();

  public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
  {
    // first check if commit is the same and preview objects have already been generated
    Commit commit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
    progress.Report = new ProgressReport();

    if (commit.id != SelectedReceiveCommit)
    {
      // check for converter
      var converter = KitManager.GetDefaultKit().LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);

      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
      {
        settings.Add(setting.Slug, setting.Selection);
      }

      settings["preview"] = "true";
      converter.SetConverterSettings(settings);

      var commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);

      Preview.Clear();
      StoredObjects.Clear();

      Preview = FlattenCommitObject(commitObject, converter);
      foreach (var previewObj in Preview)
      {
        progress.Report.Log(previewObj);
      }

      IConvertedObjectsCache<Base, Element> convertedObjects = null;
      await APIContext
        .Run(app =>
        {
          using (var t = new Transaction(CurrentDoc.Document, $"Baking stream {state.StreamId}"))
          {
            t.Start();
            convertedObjects = ConvertReceivedObjects(converter, progress, new TransactionManager(null, null));
            t.Commit();
          }

          AddMultipleRevitElementServers(convertedObjects);
        })
        .ConfigureAwait(false);
    }
    else // just generate the log
    {
      foreach (var previewObj in Preview)
      {
        progress.Report.Log(previewObj);
      }
    }

    return null;
  }

  public override void ResetDocument()
  {
    UnregisterServers();
  }

  public void AddMultipleRevitElementServers(IConvertedObjectsCache<Base, Element> convertedObjects)
  {
    ExternalService directContext3DService = ExternalServiceRegistry.GetService(
      ExternalServices.BuiltInExternalServices.DirectContext3DService
    );
    MultiServerService msDirectContext3DService = directContext3DService as MultiServerService;
    IList<Guid> serverIds = msDirectContext3DService.GetActiveServerIds();

    foreach (var obj in convertedObjects.GetConvertedObjects())
    {
      if (obj is not IDirectContext3DServer server)
      {
        continue;
      }

      directContext3DService.AddServer(server);
      m_servers.Add(server);
      serverIds.Add(server.GetServerId());
      //RefreshView();
    }

    msDirectContext3DService.SetActiveServers(serverIds);

    //m_documents.Add(uidoc.Document);
    CurrentDoc.UpdateAllOpenViews();
  }

  public void UnregisterServers()
  {
    ExternalServiceId externalDrawerServiceId = ExternalServices.BuiltInExternalServices.DirectContext3DService;
    var externalDrawerService = ExternalServiceRegistry.GetService(externalDrawerServiceId) as MultiServerService;
    if (externalDrawerService == null)
    {
      return;
    }

    foreach (var registeredServerId in externalDrawerService.GetRegisteredServerIds())
    {
      var externalDrawServer = externalDrawerService.GetServer(registeredServerId) as IDirectContext3DServer;
      if (externalDrawServer == null)
      {
        continue;
      }
      //if (document != null && !document.Equals(externalDrawServer.Document))
      //  continue;
      externalDrawerService.RemoveServer(registeredServerId);
    }

    m_servers.Clear();
    CurrentDoc.UpdateAllOpenViews();
  }

  public override bool CanPreviewSend => true;

  public override void PreviewSend(StreamState state, ProgressViewModel progress)
  {
    var converter = (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
    var filterObjs = GetSelectionFilterObjects(converter, state.Filter);
    foreach (var filterObj in filterObjs)
    {
      var descriptor = ConnectorRevitUtils.ObjectDescriptor(filterObj);
      var reportObj = new ApplicationObject(filterObj.UniqueId, descriptor);
      if (!converter.CanConvertToSpeckle(filterObj))
      {
        reportObj.Update(
          status: ApplicationObject.State.Skipped,
          logItem: $"Sending this object type is not supported in Revit"
        );
      }
      else
      {
        reportObj.Update(status: ApplicationObject.State.Created);
      }

      progress.Report.Log(reportObj);
    }

    SelectClientObjects(filterObjs.Select(o => o.UniqueId).ToList(), true);
  }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using DUI3;
using DUI3.Bindings;
using DUI3.Models.Card;
using DUI3.Operations;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace AutocadCivilDUI3Shared.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";

  public IBridge Parent { get; set; }

  private readonly AutocadDocumentModelStore _store;

  private Document Doc => Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public SendBinding(AutocadDocumentModelStore store)
  {
    _store = store;

    Database db = HostApplicationServices.WorkingDatabase;
    db.ObjectAppended += (_, e) => OnChangeChangedObjectIds(e.DBObject);
    db.ObjectErased += (_, e) => OnChangeChangedObjectIds(e.DBObject);
    db.ObjectModified += (_, e) => OnChangeChangedObjectIds(e.DBObject);
  }

  private void OnChangeChangedObjectIds(DBObject dBObject)
  {
    if (!_store.IsDocumentInit)
    {
      return;
    }

    ChangedObjectIds.Add(dBObject.Id.ToString());
    RunExpirationChecks();
  }

  public List<ISendFilter> GetSendFilters() => new() { new AutocadEverythingFilter(), new AutocadSelectionFilter() };

  public CancellationManager CancellationManager { get; } = new();

  public async void Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;

      // 2 - Check account exist
      Account account = Accounts.GetAccount(model.AccountId);

      // 3 - Get elements to convert
      List<DBObject> dbObjects = Objects.GetObjectsFromDocument(Doc, model.SendFilter.GetObjectIds());

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(Doc, Utils.Utils.VersionedAppName);

      // 5 - Convert objects
      Base commitObject = ConvertObjects(dbObjects, converter, modelCardId, cts);

      if (cts.IsCancellationRequested)
      {
        return;
      }

      // 6 - Get transports
      List<ITransport> transports = new() { new ServerTransport(account, model.ProjectId) };

      // 7 - Serialize and Send objects
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Uploading..." });
      string objectId = await Speckle.Core.Api.Operations
        .Send(commitObject, cts.Token, transports, disposeTransports: true)
        .ConfigureAwait(true);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 8 - Create Version
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Linking version to model..." });
      
      // 8 - Create the version (commit)
      var apiClient = new Client(account);
      string versionId = await apiClient.CommitCreate(new CommitCreateInput()
      {
        streamId = model.ProjectId, branchName = model.ModelId, sourceApplication = "Rhino", objectId = objectId
      }, cts.Token).ConfigureAwait(true);
      
      SendBindingUiCommands.SetModelCreatedVersionId(Parent, modelCardId, versionId);
      apiClient.Dispose();
    }
    catch (Exception e)
    {
      if (e is OperationCanceledException)
      {
        return;
      }
      
      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId, e);
    }
  }

  public void CancelSend(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  private void RunExpirationChecks()
  {
    List<SenderModelCard> senders = _store.GetSenders();
    string[] objectIdsList = ChangedObjectIds.ToArray();
    List<string> expiredSenderIds = new();

    foreach (SenderModelCard sender in senders)
    {
      bool isExpired = sender.SendFilter.CheckExpiry(objectIdsList);
      if (isExpired)
      {
        expiredSenderIds.Add(sender.ModelCardId);
      }
    }
    
    SendBindingUiCommands.SetModelsExpired(Parent, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  private Base ConvertObjects(
    List<DBObject> dbObjects,
    ISpeckleConverter converter,
    string modelCardId,
    CancellationTokenSource cts
  )
  {
    Base commitObject = new();

    List<Base> convertedObjects = new();
    int count = 0;
    foreach (DBObject obj in dbObjects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }
      count++;

      try
      {
        // convert obj
        Base converted = converter.ConvertToSpeckle(obj);
        if (converted == null)
        {
          // TODO: report!
          continue;
        }

        convertedObjects.Add(converted);
        double progress = (double)count / dbObjects.Count;
        BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress() {Status = "Converting", Progress = progress});
      }
      catch (SpeckleException e)
      {
        Debug.WriteLine(e.Message);
        // FIXME: Figure it out why it's happening!
        continue;
      }
    }

    commitObject["@elements"] = convertedObjects;

    return commitObject;
  }
}

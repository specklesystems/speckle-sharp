using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Models.Card;
using DUI3.Operations;
using DUI3.Settings;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Operations = DUI3.Operations.Operations;

namespace ConnectorRhinoWebUI.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }
  private readonly DocumentModelStore _store;
  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public SendBinding(DocumentModelStore store)
  {
    _store = store;
    
    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      SendBindingUiCommands.RefreshSendFilters(Parent);
    };

    RhinoDoc.AddRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      ChangedObjectIds.Add(e.OldRhinoObject.Id.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>()
    {
      new RhinoEverythingFilter(), 
      new RhinoSelectionFilter() { IsDefault = true }, 
      new RhinoLayerFilter()
    };
  }

  public List<CardSetting> GetSendSettings()
  {
    return new List<CardSetting>()
    {
      new()
      {
        Id = "includeAttributes",
        Title = "Include Attributes",
        Value = true,
        Type = "boolean"
      },
    };
  }

  public async void Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model

      if (_store.GetModelById(modelCardId) is not SenderModelCard model)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }
      
      // 2 - Check account exist
      Account account = Accounts.GetAccount(model.AccountId);

      // 3 - Get elements to convert, throw early if nothing is selected
      List<RhinoObject> rhinoObjects = GetObjectsFromDocument(model);

      if (rhinoObjects.Count == 0)
      {
        throw new InvalidOperationException("No objects were found. Please update your send filter!");
      }

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(RhinoDoc.ActiveDoc, "Rhino7");

      // 5 - Convert objects
      Base commitObject = ConvertObjects(rhinoObjects, converter, modelCardId, cts);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 6 - Get transports
      List<ITransport> transports = new() { new ServerTransport(account, model.ProjectId) };

      // 7 - Serialize and Send objects
      SendBindingUiCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Uploading..." });
      string objectId = await Speckle.Core.Api.Operations
        .Send(commitObject, cts.Token, transports, disposeTransports: true)
        .ConfigureAwait(true);
      
      SendBindingUiCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Linking version to model..." });
      
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
      if (e is OperationCanceledException) // We do not want to display an error, we just stop sending.
      {
        return;
      }

      SendBindingUiCommands.SetModelError(Parent, modelCardId, e);
    }
  }

  public void CancelSend(string modelCardId) => CancellationManager.CancelOperation(modelCardId);
  private Base ConvertObjects(
    List<RhinoObject> rhinoObjects,
    ISpeckleConverter converter,
    string modelCardId,
    CancellationTokenSource cts
  )
  {
    Base commitObject = new();
  
    List<Base> convertedObjects = new();
    int count = 0;
    
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      count++;
      var converted = converter.ConvertToSpeckle(rhinoObject);
      converted.applicationId = rhinoObject.Id.ToString();
      convertedObjects.Add(converted);
      double progress = (double)count / rhinoObjects.Count;
      
      SendBindingUiCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress(){ Status = "Converting", Progress = progress});
      
      Thread.Sleep(550);
    }

    commitObject["@elements"] = convertedObjects;

    return commitObject;
  }

  private List<RhinoObject> GetObjectsFromDocument(SenderModelCard model)
  {
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    return objectsIds.Select((id) => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id))).Where(obj => obj!=null).ToList();
  }
  
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

}

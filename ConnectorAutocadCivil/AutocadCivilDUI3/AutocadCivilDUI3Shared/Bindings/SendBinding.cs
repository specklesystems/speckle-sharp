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
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace AutocadCivilDUI3Shared.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";

  public IBridge Parent { get; set; }

  private readonly AutocadDocumentModelStore _store;

  private Document Doc => Application.DocumentManager.MdiActiveDocument;

  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public SendBinding(AutocadDocumentModelStore store)
  {
    _store = store;
    Application.DocumentManager.DocumentActivated += (sender, args) => SubscribeToObjectChanges(args.Document);
  }

  private readonly List<string> _docSubsTracker = new();
  private void SubscribeToObjectChanges(Document doc)
  {
    if (doc == null || doc.Database == null || _docSubsTracker.Contains(doc.Name))
    {
      return;
    }

    _docSubsTracker.Add(doc.Name);
    doc.Database.ObjectAppended += (_, e) => OnChangeChangedObjectIds(e.DBObject);
    doc.Database.ObjectErased += (_, e) => OnChangeChangedObjectIds(e.DBObject);
    doc.Database.ObjectModified += (_, e) => OnChangeChangedObjectIds(e.DBObject);
  }
  
  private void OnChangeChangedObjectIds(DBObject dBObject)
  { 
    ChangedObjectIds.Add(dBObject.Handle.Value.ToString());
    AutocadIdleManager.SubscribeToIdle(RunExpirationChecks);
  }

  public List<ISendFilter> GetSendFilters() => new() { new AutocadEverythingFilter(), new AutocadSelectionFilter() };

  public CancellationManager CancellationManager { get; } = new();

  public void Send(string modelCardId)
  {
    Parent.RunOnMainThread(() => SendInternal(modelCardId));
  }

  private async void SendInternal(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Setup
      var model = _store.GetModelById(modelCardId) as SenderModelCard;
      var account = Accounts.GetAccount(model.AccountId);
      var converter = Converters.GetConverter(Doc, Utils.Utils.VersionedAppName);

      // 2 - Get elements to convert
      var dbObjects = Objects.GetObjectsFromDocument(Doc, model.SendFilter.GetObjectIds());
      if (dbObjects.Count == 0)
      {
        throw new InvalidOperationException("No objects were found. Please update your send filter!");
      }
      // 5 - Convert objects
      var commitObject = ConvertObjects(dbObjects, converter, modelCardId, cts);

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
    catch (Exception e) // NOTE: Always catch everything we can!
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
    List<(DBObject obj, string layer, string applicationId)> dbObjects,
    ISpeckleConverter converter,
    string modelCardId,
    CancellationTokenSource cts
  )
  {
    var modelWithLayers = new Collection() { name = Doc.Name.Split( new [] {"\\"}, StringSplitOptions.None).Reverse().First(), collectionType = "root" };
    var collectionCache = new Dictionary<string, Collection>();
    int count = 0;

    foreach (var tuple in dbObjects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }
      try
      {
        Base converted = converter.ConvertToSpeckle(tuple.obj);
        converted.applicationId = tuple.applicationId;

        if (converted == null)
        {
          // TODO: report, error out, etc.
        }
        
        // Create and add a collection for each layer if not done so already.
        if (!collectionCache.ContainsKey(tuple.layer))
        {
          collectionCache[tuple.layer] = new Collection() { name = tuple.layer, collectionType = "layer" };
          modelWithLayers.elements.Add(collectionCache[tuple.layer]);
        }
        
        collectionCache[tuple.layer].elements.Add(converted);

        BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress() { Status = "Converting", Progress = (double)++count / dbObjects.Count});
      }
      catch (Exception e) // THE FUCK
      {
        // TODO: Add to report, etc.
        Debug.WriteLine(e.Message);
      }
    }

    return modelWithLayers;
  }
}

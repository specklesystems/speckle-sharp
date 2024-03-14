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
using Speckle.Core.Logging;
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

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  /// <summary>
  /// Keeps track of previously converted objects as a dictionary of (applicationId, object reference).
  /// </summary>
  private readonly Dictionary<string, ObjectReference> _convertedObjectReferences = new();

  public SendBinding(AutocadDocumentModelStore store)
  {
    _store = store;
    Application.DocumentManager.DocumentActivated += (sender, args) => SubscribeToObjectChanges(args.Document);
    if (Application.DocumentManager.CurrentDocument != null)
    {
      // NOTE: catches the case when autocad just opens up with a blank new doc
      SubscribeToObjectChanges(Application.DocumentManager.CurrentDocument);
    }
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
      var modelCard = _store.GetModelById(modelCardId) as SenderModelCard;
      var account = Accounts.GetAccount(modelCard.AccountId);
      var converter = Converters.GetConverter(Doc, Utils.Utils.VersionedAppName);

      // 2 - Get elements to convert
      var dbObjects = Objects.GetObjectsFromDocument(Doc, modelCard.SendFilter.GetObjectIds());
      if (dbObjects.Count == 0)
      {
        throw new InvalidOperationException("No objects were found. Please update your send filter!");
      }
      // 5 - Convert objects
      var commitObject = ConvertObjects(dbObjects, converter, modelCard, cts);

      if (cts.IsCancellationRequested)
      {
        return;
      }

      // 7 - Serialize and Send objects
      var transport = new ServerTransport(account, modelCard.ProjectId);
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress { Status = "Uploading..." }
      );
      var sendResult = await SendHelper.Send(commitObject, transport, true, null, cts.Token).ConfigureAwait(true);

      // Store the converted references in memory for future send operations, overwriting the existing values for the given application id.
      foreach (var kvp in sendResult.convertedReferences)
      {
        _convertedObjectReferences[kvp.Key + modelCard.ProjectId] = kvp.Value;
      }
      // It's important to reset the model card's list of changed obj ids so as to ensure we accurately keep track of changes between send operations.
      modelCard.ChangedObjectIds = new();

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 8 - Create Version
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress { Status = "Linking version to model..." }
      );

      // 8 - Create the version (commit)
      var apiClient = new Client(account);
      string versionId = await apiClient
        .CommitCreate(
          new CommitCreateInput()
          {
            streamId = modelCard.ProjectId,
            branchName = modelCard.ModelId,
            sourceApplication = "Rhino",
            objectId = sendResult.rootObjId
          },
          cts.Token
        )
        .ConfigureAwait(true);

      SendBindingUiCommands.SetModelCreatedVersionId(Parent, modelCardId, versionId);
      apiClient.Dispose();
    }
    catch (Exception e) when (!e.IsFatal()) // All exceptions should be handled here if possible, otherwise we enter "crashing the host app" territory.
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

    foreach (SenderModelCard modelCard in senders)
    {
      var intersection = modelCard.SendFilter.GetObjectIds().Intersect(objectIdsList).ToList();
      bool isExpired = intersection.Any();
      if (isExpired)
      {
        expiredSenderIds.Add(modelCard.ModelCardId);
        modelCard.ChangedObjectIds.UnionWith(intersection);
      }
    }

    SendBindingUiCommands.SetModelsExpired(Parent, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  private Base ConvertObjects(
    List<(DBObject obj, string layer, string applicationId)> dbObjects,
    ISpeckleConverter converter,
    SenderModelCard modelCard,
    CancellationTokenSource cts
  )
  {
    var modelWithLayers = new Collection()
    {
      name = Doc.Name.Split(new[] { "\\" }, StringSplitOptions.None).Reverse().First(),
      collectionType = "root"
    };
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
        Base converted;
        var applicationId = tuple.applicationId;

        if (
          !modelCard.ChangedObjectIds.Contains(applicationId)
          && _convertedObjectReferences.TryGetValue(applicationId + modelCard.ProjectId, out ObjectReference value)
        )
        {
          converted = value;
        }
        else
        {
          converted = converter.ConvertToSpeckle(tuple.obj);
          converted.applicationId = applicationId;
        }

        // Create and add a collection for each layer if not done so already.
        if (!collectionCache.ContainsKey(tuple.layer))
        {
          collectionCache[tuple.layer] = new Collection() { name = tuple.layer, collectionType = "layer" };
          modelWithLayers.elements.Add(collectionCache[tuple.layer]);
        }

        collectionCache[tuple.layer].elements.Add(converted);

        BasicConnectorBindingCommands.SetModelProgress(
          Parent,
          modelCard.ModelCardId,
          new ModelCardProgress() { Status = "Converting", Progress = (double)++count / dbObjects.Count }
        );
      }
      catch (Exception e) when (!e.IsFatal())
      {
        // TODO: Add to report, etc.
        Debug.WriteLine(e.Message);
      }
    }

    return modelWithLayers;
  }
}

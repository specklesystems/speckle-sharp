using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using DUI3;
using DUI3.Bindings;
using DUI3.Models.Card;
using DUI3.Operations;
using Speckle.ConnectorRevitDUI3.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;
using Speckle.Core.Models;
using DUI3.Utils;
using RevitSharedResources.Interfaces;
using Speckle.Core.Api;

namespace Speckle.ConnectorRevitDUI3.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }

  private readonly RevitDocumentStore _store;

  private static UIApplication s_revitApp;

  private static Document Doc => s_revitApp.ActiveUIDocument.Document;

  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> ChangedObjectIds { get; set; } = new();
  
  private static IRevitDocumentAggregateCache revitDocumentAggregateCache;

  public SendBinding(RevitDocumentStore store)
  {
    s_revitApp = RevitAppProvider.RevitApp;
    _store = store;
    
    s_revitApp.Application.DocumentChanged += (_, e) => StoreChangedElementIdsAndScheduleExpiryCheck(e);
    revitDocumentAggregateCache = new RevitDocumentAggregateCache(new UIDocumentProvider(s_revitApp));
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new RevitSelectionFilter() {IsDefault = true} };
  }

  
  public async void Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      SenderModelCard modelCard = _store.GetModelById(modelCardId) as SenderModelCard;

      // 2 - Check account exist
      Account account = Accounts.GetAccount(modelCard.AccountId);

      // 3 - Get elements to convert
      var ids = modelCard.SendFilter.GetObjectIds();
      var elements = new List<Element>();
      foreach (var id in ids)
      {
        var el = Doc.GetElement(ElementId.Parse(id));
        if (el != null)
        {
          elements.Add(el);
        }
      }
      // List<Element> elements = Utils.Elements.GetElementsFromDocument(Doc, modelCard.SendFilter.GetObjectIds());

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(Doc, RevitAppProvider.Version());
      converter.SetContextDocument(revitDocumentAggregateCache);
      
      // 5 - Convert objects
      Base commitObject = ConvertElements(elements, converter, modelCardId, cts);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 6 - Get transports
      List<ITransport> transports = new() { new ServerTransport(account, modelCard.ProjectId) };

      // 7 - Serialize and Send objects
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Uploading..." });
      string objectId = await Speckle.Core.Api.Operations
        .Send(commitObject, cts.Token, transports, disposeTransports: true)
        .ConfigureAwait(true);
      
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Linking version to model..." });
      
      // 8 - Create Version
      var apiClient = new Client(account);
      string versionId = await apiClient.CommitCreate(new CommitCreateInput()
      {
        streamId = modelCard.ProjectId, branchName = modelCard.ModelId, sourceApplication = "Revit", objectId = objectId
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
  
  private Base ConvertElements(
    List<Element> elements,
    ISpeckleConverter converter,
    string modelCardId,
    CancellationTokenSource cts
  )
  {
    int count = 0;
    var rootObject = new Collection { name = Doc.PathName.Split('\\').Reverse().First().Split('.').First() };
    var collectionCache = new Dictionary<string, Collection>();
    
    foreach (var revitElement in elements)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException();
      }

      var cat = revitElement.Category.Name;
      var level = Doc.GetElement(revitElement.LevelId) as Level;
      var path = new[] { level == null ? "No level": level.Name, cat };
      var collection = GetAndCreateObjectHostCollection(path, collectionCache, rootObject);

      count++;
      try
      {
        var convertedObject = converter.ConvertToSpeckle(revitElement);
        convertedObject.applicationId = revitElement.UniqueId;
        collection.elements.Add(convertedObject);

        BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress() { Status = "Converting", Progress = (double)count / elements.Count });
      }
      catch (Exception e)
      {
        // TODO: Add to report, etc.
        Debug.WriteLine(e.Message);
      }
      
    }
    return rootObject;
  }

  /// <summary>
  /// Creates and nests collections based on the provided path within the root collection provided. This will not return a new collection each time is called, but an existing one if one is found.
  /// For example, you can use this to use (or re-use) a new collection for a path of (level, category) as it's currently implemented. 
  /// </summary>
  /// <param name="path"></param>
  /// <param name="cache"></param>
  /// <param name="root"></param>
  /// <returns></returns>
  private Collection GetAndCreateObjectHostCollection(IEnumerable<string> path, Dictionary<string, Collection> cache, Collection root)
  {
    string fullPathName = string.Join("", path);
    if (cache.TryGetValue(fullPathName, out Collection value))
    {
      return value;
    }
    
    string flatPathName = "";
    Collection previousCollection = root;
    
    foreach (var pathItem in path)
    {
      flatPathName += pathItem;
      Collection childCollection;
      if (cache.ContainsKey(flatPathName))
      {
        childCollection = cache[flatPathName];
      }
      else
      {
        childCollection = new Collection(pathItem, "layer");
        previousCollection.elements.Add(childCollection);
        cache[flatPathName] = childCollection;
      }

      previousCollection = childCollection;
    }

    return previousCollection;
  }
  
  /// <summary>
  /// Keeps track of the changed element ids as well as checks if any of them need to trigger
  /// a filter refresh (e.g., views being added).
  /// </summary>
  /// <param name="e"></param>
  private void StoreChangedElementIdsAndScheduleExpiryCheck(Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
  {
    ICollection<ElementId> addedElementIds = e.GetAddedElementIds();
    ICollection<ElementId> deletedElementIds = e.GetDeletedElementIds();
    ICollection<ElementId> modifiedElementIds = e.GetModifiedElementIds();

    foreach (ElementId elementId in addedElementIds)
    {
      ChangedObjectIds.Add(elementId.ToString());
    }

    foreach (ElementId elementId in deletedElementIds)
    {
      ChangedObjectIds.Add(elementId.ToString());
    }

    foreach (ElementId elementId in modifiedElementIds)
    {
      ChangedObjectIds.Add(elementId.ToString());
    }

    // TODO: CHECK IF ANY OF THE ABOVE ELEMENTS NEED TO TRIGGER A FILTER REFRESH
    RevitIdleManager.SubscribeToIdle(RunExpirationChecks);
  }

  private void RunExpirationChecks()
  {
    List<SenderModelCard> senders = _store.GetSenders();
    List<string> expiredSenderIds = new();

    foreach (var sender in senders)
    {
      bool isExpired = sender.SendFilter.CheckExpiry(ChangedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(sender.ModelCardId);
      }
    }
    SendBindingUiCommands.SetModelsExpired(Parent, expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }
}

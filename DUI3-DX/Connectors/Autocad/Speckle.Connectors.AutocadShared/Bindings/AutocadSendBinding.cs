using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.Filters;
using Speckle.Connectors.Autocad.Utils;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Core.Transports;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Api;
using Speckle.Core.Models;
using System.Diagnostics;
using ICancelable = System.Reactive.Disposables.ICancelable;

namespace Speckle.Connectors.Autocad.Bindings;

public class AutocadSendBinding : ISendBinding, ICancelable
{
  public string Name { get; } = "sendBinding";
  public SendBindingUICommands Commands { get; }
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly AutocadIdleManager _idleManager;
  private readonly AutocadSettings _autocadSettings;
  private readonly AutocadContext _autocadContext;
  private readonly ISpeckleConverterToSpeckle _toSpeckleConverter;
  private readonly CancellationManager _cancellationManager;
  private readonly IScopedFactory<ISpeckleConverterToSpeckle> _speckleConverterToSpeckleFactory;

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public AutocadSendBinding(
    DocumentModelStore store,
    AutocadIdleManager idleManager,
    AutocadSettings autocadSettings,
    IBridge parent,
    IBasicConnectorBinding basicConnectorBinding,
    CancellationManager cancellationManager,
    IScopedFactory<ISpeckleConverterToSpeckle> speckleConverterToSpeckleFactory,
    AutocadContext autocadContext
  )
  {
    _store = store;
    _idleManager = idleManager;
    _autocadSettings = autocadSettings;
    _speckleConverterToSpeckleFactory = speckleConverterToSpeckleFactory;
    _autocadContext = autocadContext;
    _cancellationManager = cancellationManager;
    _toSpeckleConverter = _speckleConverterToSpeckleFactory.ResolveScopedInstance();

    Parent = parent;
    Commands = new SendBindingUICommands(parent);

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
    _idleManager.SubscribeToIdle(RunExpirationChecks);
  }

  private void RunExpirationChecks()
  {
    List<SenderModelCard> senders = _store.GetSenders();
    string[] objectIdsList = ChangedObjectIds.ToArray();
    List<string> expiredSenderIds = new();

    foreach (SenderModelCard modelCard in senders)
    {
      var intersection = modelCard.SendFilter.GetObjectIds().Intersect(objectIdsList).ToList();
      bool isExpired = intersection.Count != 0;
      if (isExpired)
      {
        expiredSenderIds.Add(modelCard.ModelCardId);
        modelCard.ChangedObjectIds.UnionWith(intersection);
      }
    }

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new AutocadSelectionFilter { IsDefault = true } };
  }

  public async Task Send(string modelCardId)
  {
    Parent.RunOnMainThread(async () => await SendInternal(modelCardId).ConfigureAwait(false));
  }

  private async Task SendInternal(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      // 2 - Check account exist
      // TODO: FETCHING ACCOUNTS BY ID ONLY IS UNSAFE
      Account account =
        AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == modelCard.AccountId)
        ?? throw new SpeckleAccountManagerException();

      // 3 - Get elements to convert
      List<(DBObject obj, string applicationId)> autocadObjects =
        Application.DocumentManager.CurrentDocument.GetObjects(modelCard.SendFilter.GetObjectIds());
      if (autocadObjects.Count == 0)
      {
        throw new InvalidOperationException("No objects were found. Please update your send filter!");
      }

      // 4 - Convert objects
      Base commitObject = ConvertObjects(autocadObjects, modelCard, cts.Token);

      cts.Token.ThrowIfCancellationRequested();

      // 5 - Serialize and Send objects
      Commands.SetModelProgress(modelCardId, new ModelCardProgress { Status = "Uploading..." });

      var transport = new ServerTransport(account, modelCard.ProjectId);
      var sendResult = await SendHelper.Send(commitObject, transport, true, null, cts.Token).ConfigureAwait(true);

      // Store the converted references in memory for future send operations, overwriting the existing values for the given application id.
      // foreach (var kvp in sendResult.convertedReferences)
      // {
      //   // TODO: Bug in here, we need to encapsulate cache not only by app id, but also by project id,
      //   // TODO: as otherwise we assume incorrectly that an object exists for a given project (e.g, send box to project 1, send same unchanged box to project 2)
      //   _convertedObjectReferences[kvp.Key + modelCard.ProjectId] = kvp.Value;
      // }
      // It's important to reset the model card's list of changed obj ids so as to ensure we accurately keep track of changes between send operations.
      // NOTE: ChangedObjectIds is currently JsonIgnored, but could actually be useful for highlighting changes in host app.
      //modelCard.ChangedObjectIds = new();

      // 7 - Create Version
      Commands.SetModelProgress(modelCardId, new ModelCardProgress { Status = "Linking version to model..." });

      // 8 - Create the version (commit)
      Client apiClient = new(account);
      string versionId = await apiClient
        .CommitCreate(
          new CommitCreateInput
          {
            streamId = modelCard.ProjectId,
            branchName = modelCard.ModelId,
            sourceApplication = "Autocad",
            objectId = sendResult.rootObjId
          },
          cts.Token
        )
        .ConfigureAwait(true);

      Commands.SetModelCreatedVersionId(modelCardId, versionId);
      apiClient.Dispose();
    }
    catch (OperationCanceledException)
    {
      return;
    }
    catch (Exception e) when (!e.IsFatal()) // All exceptions should be handled here if possible, otherwise we enter "crashing the host app" territory.
    {
      Commands.SetModelError(modelCardId, e);
    }
  }

  private Base ConvertObjects(
    List<(DBObject obj, string applicationId)> dbObjects,
    SenderModelCard modelCard,
    CancellationToken cancellationToken
  )
  {
    ISpeckleConverterToSpeckle converter = _speckleConverterToSpeckleFactory.ResolveScopedInstance();
    Collection modelWithLayers =
      new()
      {
        name = Application.DocumentManager.CurrentDocument.Name
          .Split(s_separator, StringSplitOptions.None)
          .Reverse()
          .First(),
        collectionType = "root"
      };

    Dictionary<string, Collection> collectionCache = new();
    int count = 0;

    foreach ((DBObject obj, string applicationId) tuple in dbObjects)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var dbObject = tuple.obj;
      var applicationId = tuple.applicationId;

      // 3. get from cache or convert:
      // What we actually do here is check if the object has been previously converted AND has not changed.
      // If that's the case, we insert in the host collection just its object reference which has been saved from the prior conversion.
      /*Base converted;
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
      }*/
      try
      {
        Base? converted = converter.Convert(dbObject);
        if (converted != null)
        {
          converted.applicationId = applicationId;

          // Create and add a collection for each layer if not done so already.
          if ((tuple.obj as Entity)?.Layer is string layer)
          {
            if (!collectionCache.TryGetValue(layer, out Collection? collection))
            {
              collection = new Collection() { name = layer, collectionType = "layer" };
              collectionCache[layer] = collection;
              modelWithLayers.elements.Add(collectionCache[layer]);
            }

            collection.elements.Add(converted);
          }
        }
        Commands.SetModelProgress(
          modelCard.ModelCardId,
          new ModelCardProgress() { Status = "Converting", Progress = (double)++count / dbObjects.Count }
        );
      }
      catch (SpeckleConversionException e)
      {
        // DO something with the exception
        Console.WriteLine(e);
      }
      catch (NotSupportedException e)
      {
        // DO something with the exception
        Console.WriteLine(e);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        // TODO: Add to report, etc.
        Debug.WriteLine(e.Message);
      }
    }

    return modelWithLayers;
  }

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public void Dispose()
  {
    IsDisposed = true;
    _speckleConverterToSpeckleFactory.Dispose();
  }

  public bool IsDisposed { get; private set; }

  private static readonly string[] s_separator = new[] { "\\" };
}

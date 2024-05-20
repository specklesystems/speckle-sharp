using System.Diagnostics.CodeAnalysis;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.ArcGis.Operations.Send;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Core.Logging;
using ICancelable = System.Reactive.Disposables.ICancelable;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.Settings;
using Speckle.Connectors.Utils;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.ArcGIS.Filters;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data;

namespace Speckle.Connectors.ArcGIS.Bindings;

public sealed class ArcGISSendBinding : ISendBinding, ICancelable
{
  public string Name => "sendBinding";
  public SendBindingUICommands Commands { get; }
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly IUnitOfWorkFactory _unitOfWorkFactory; // POC: unused? :D
  private readonly List<ISendFilter> _sendFilters;
  private readonly CancellationManager _cancellationManager;

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();
  private List<FeatureLayer> SubscribedLayers { get; set; } = new();
  private List<StandaloneTable> SubscribedTables { get; set; } = new();

  public ArcGISSendBinding(
    DocumentModelStore store,
    IBridge parent,
    IEnumerable<ISendFilter> sendFilters,
    IUnitOfWorkFactory unitOfWorkFactory,
    CancellationManager cancellationManager
  )
  {
    _store = store;
    _unitOfWorkFactory = unitOfWorkFactory;
    _sendFilters = sendFilters.ToList();
    _cancellationManager = cancellationManager;

    Parent = parent;
    Commands = new SendBindingUICommands(parent);
    SubscribeToArcGISEvents();
  }

  private void SubscribeToArcGISEvents()
  {
    LayersRemovedEvent.Subscribe(GetIdsForLayersRemovedEvent, true);
    StandaloneTablesRemovedEvent.Subscribe(GetIdsForStandaloneTablesRemovedEvent, true);
    MapPropertyChangedEvent.Subscribe(GetIdsForMapPropertyChangedEvent, true); // Map units, CRS etc.
    MapMemberPropertiesChangedEvent.Subscribe(GetIdsForMapMemberPropertiesChangedEvent, true); // e.g. Layer name

    ActiveMapViewChangedEvent.Subscribe(SubscribeToMapMembersDataSourceChange, true);
    LayersAddedEvent.Subscribe(GetIdsForLayersAddedEvent, true);
    StandaloneTablesAddedEvent.Subscribe(GetIdsForStandaloneTablesAddedEvent, true);
  }

  private void SubscribeToMapMembersDataSourceChange(ActiveMapViewChangedEventArgs args)
  {
    var task = QueuedTask.Run(() =>
    {
      if (MapView.Active == null)
      {
        return;
      }

      // subscribe to layers
      foreach (Layer layer in MapView.Active.Map.Layers)
      {
        if (layer is FeatureLayer featureLayer)
        {
          SubscribeToFeatureLayerDataSourceChange(featureLayer);
        }
      }
      // subscribe to tables
      foreach (StandaloneTable table in MapView.Active.Map.StandaloneTables)
      {
        SubscribeToTableDataSourceChange(table);
      }
    });
    task.Wait();
  }

  private void SubscribeToFeatureLayerDataSourceChange(FeatureLayer layer)
  {
    if (SubscribedLayers.Contains(layer))
    {
      return;
    }
    Table layerTable = layer.GetTable();
    SubscribeToAnyDataSourceChange(layerTable);
    SubscribedLayers.Add(layer);
  }

  private void SubscribeToTableDataSourceChange(StandaloneTable table)
  {
    if (SubscribedTables.Contains(table))
    {
      return;
    }
    Table layerTable = table.GetTable();
    SubscribeToAnyDataSourceChange(layerTable);
    SubscribedTables.Add(table);
  }

  private void SubscribeToAnyDataSourceChange(Table layerTable)
  {
    RowCreatedEvent.Subscribe(
      (args) =>
      {
        OnRowChanged(args);
      },
      layerTable
    );
    RowChangedEvent.Subscribe(
      (args) =>
      {
        OnRowChanged(args);
      },
      layerTable
    );
    RowDeletedEvent.Subscribe(
      (args) =>
      {
        OnRowChanged(args);
      },
      layerTable
    );
  }

  private void OnRowChanged(RowChangedEventArgs args)
  {
    if (args == null || MapView.Active == null)
    {
      return;
    }

    // get the path of the edited dataset
    var datasetURI = args.Row.GetTable().GetPath();

    // find all layers & tables reading from the dataset
    foreach (Layer layer in MapView.Active.Map.Layers)
    {
      if (layer.GetPath() == datasetURI)
      {
        ChangedObjectIds.Add(layer.URI);
      }
    }
    foreach (StandaloneTable table in MapView.Active.Map.StandaloneTables)
    {
      if (table.GetPath() == datasetURI)
      {
        ChangedObjectIds.Add(table.URI);
      }
    }
  }

  private void GetIdsForLayersRemovedEvent(LayerEventsArgs args)
  {
    foreach (Layer layer in args.Layers)
    {
      ChangedObjectIds.Add(layer.URI);
    }
    RunExpirationChecks(true);
  }

  private void GetIdsForStandaloneTablesRemovedEvent(StandaloneTableEventArgs args)
  {
    foreach (StandaloneTable table in args.Tables)
    {
      ChangedObjectIds.Add(table.URI);
    }
    RunExpirationChecks(true);
  }

  private void GetIdsForMapPropertyChangedEvent(MapPropertyChangedEventArgs args)
  {
    foreach (Map map in args.Maps)
    {
      foreach (MapMember member in map.Layers)
      {
        ChangedObjectIds.Add(member.URI);
      }
    }
    RunExpirationChecks(false);
  }

  private void GetIdsForLayersAddedEvent(LayerEventsArgs args)
  {
    foreach (Layer layer in args.Layers)
    {
      if (layer is FeatureLayer featureLayer)
      {
        SubscribeToFeatureLayerDataSourceChange(featureLayer);
      }
    }
  }

  private void GetIdsForStandaloneTablesAddedEvent(StandaloneTableEventArgs args)
  {
    foreach (StandaloneTable table in args.Tables)
    {
      SubscribeToTableDataSourceChange(table);
    }
  }
  
  private void GetIdsForMapMemberPropertiesChangedEvent(MapMemberPropertiesChangedEventArgs args)
  {
    foreach (MapMember member in args.MapMembers)
    {
      ChangedObjectIds.Add(member.URI);
    }
    RunExpirationChecks(false);
  }

  public List<ISendFilter> GetSendFilters() => _sendFilters;

  // POC: delete this
  public List<CardSetting> GetSendSettings()
  {
    return new List<CardSetting>
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

  [SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Being refactored on in parallel, muting this issue so CI can pass initially."
  )]
  public async Task Send(string modelCardId)
  {
    //poc: dupe code between connectors
    using IUnitOfWork<SendOperation> unitOfWork = _unitOfWorkFactory.Resolve<SendOperation>();
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      string versionId = await unitOfWork.Service
        .Execute(
          modelCard.SendFilter.NotNull(),
          modelCard.AccountId.NotNull(),
          modelCard.ProjectId.NotNull(),
          modelCard.ModelId.NotNull(),
          (status, progress) => OnSendOperationProgress(modelCardId, status, progress),
          cts.Token
        )
        .ConfigureAwait(false);

      Commands.SetModelCreatedVersionId(modelCardId, versionId);
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

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  /// <summary>
  /// Checks if any sender model cards contain any of the changed objects. If so, also updates the changed objects hashset for each model card - this last part is important for on send change detection.
  /// </summary>
  private void RunExpirationChecks(bool idsDeleted)
  {
    var senders = _store.GetSenders();
    List<string> expiredSenderIds = new();
    string[] objectIdsList = ChangedObjectIds.ToArray();

    foreach (SenderModelCard sender in senders)
    {
      var objIds = sender.SendFilter.NotNull().GetObjectIds();
      var intersection = objIds.Intersect(objectIdsList).ToList();
      bool isExpired = sender.SendFilter.NotNull().CheckExpiry(ChangedObjectIds.ToArray());
      if (isExpired)
      {
        expiredSenderIds.Add(sender.ModelCardId.NotNull());
        sender.ChangedObjectIds.UnionWith(intersection.NotNull());

        // Update the model card object Ids
        if (idsDeleted && sender.SendFilter is ArcGISSelectionFilter filter)
        {
          List<string> remainingObjIds = objIds.SkipWhile(x => intersection.Contains(x)).ToList();
          filter.SelectedObjectIds = remainingObjIds;
        }
      }
    }

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress(modelCardId, status, progress));
  }

  public void Dispose()
  {
    IsDisposed = true;
  }

  public bool IsDisposed { get; private set; }
}

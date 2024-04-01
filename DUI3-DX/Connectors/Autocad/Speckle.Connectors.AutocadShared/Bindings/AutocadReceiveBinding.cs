using System.Collections;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Connectors.DUI.Models.Card;
using Speckle.Converters.Common;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using Speckle.Core.Api;
using Speckle.Core.Models;
using ICancelable = System.Reactive.Disposables.ICancelable;

namespace Speckle.Connectors.Autocad.Bindings;

public sealed class AutocadReceiveBinding : IReceiveBinding, ICancelable
{
  public string Name { get; } = "receiveBinding";
  public IBridge Parent { get; }

  private readonly DocumentModelStore _store;
  private readonly CancellationManager _cancellationManager;
  private readonly AutocadLayerManager _autocadLayerManager;

  public ReceiveBindingUICommands Commands { get; }

  private readonly IScopedFactory<ISpeckleConverterToHost> _speckleConverterToHostFactory;

  public AutocadReceiveBinding(
    DocumentModelStore store,
    IBridge parent,
    CancellationManager cancellationManager,
    IScopedFactory<ISpeckleConverterToHost> speckleConverterToHostFactory,
    AutocadLayerManager autocadLayerManager
  )
  {
    _store = store;
    _speckleConverterToHostFactory = speckleConverterToHostFactory;
    _cancellationManager = cancellationManager;
    _autocadLayerManager = autocadLayerManager;

    Parent = parent;
    Commands = new ReceiveBindingUICommands(parent);
  }

  public void CancelReceive(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public Task Receive(string modelCardId)
  {
    ReceiverModelCard modelCard = _store.GetModelById(modelCardId) as ReceiverModelCard;
    Parent.RunOnMainThread(
      async () => await ReceiveInternal(modelCardId, modelCard.SelectedVersionId).ConfigureAwait(false)
    );
    return Task.CompletedTask;
  }

  private async Task ReceiveInternal(string modelCardId, string versionId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get receiver card
      if (_store.GetModelById(modelCardId) is not ReceiverModelCard modelCard)
      {
        throw new InvalidOperationException("No download model card was found.");
      }

      // 2 - Check account exist
      Account account =
        AccountManager.GetAccounts().FirstOrDefault(acc => acc.id == modelCard.AccountId)
        ?? throw new SpeckleAccountManagerException();

      // 3 - Get commit object from server
      Client apiClient = new(account);
      ServerTransport transport = new(account, modelCard.ProjectId);
      Commit? version =
        await apiClient.CommitGet(modelCard.ProjectId, versionId, cts.Token).ConfigureAwait(false)
        ?? throw new SpeckleException($"Failed to receive commit: {versionId} from server)");

      Base? commitObject =
        await Operations
          .Receive(version.referencedObject, cancellationToken: cts.Token, remoteTransport: transport)
          .ConfigureAwait(false)
        ?? throw new SpeckleException(
          $"Failed to receive commit: {version.id} objects from server: {nameof(Operations)} returned null"
        );

      apiClient.Dispose();
      cts.Token.ThrowIfCancellationRequested();

      // 4 - Convert objects
      ConvertObjects(commitObject, modelCard, cts);
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

  private List<(List<string>, Base)> GetBaseWithPath(Base commitObject, CancellationTokenSource cts)
  {
    var objectsToConvert = new List<(List<string>, Base)>();
    foreach (var (objPath, obj) in commitObject.TraverseWithPath((obj) => obj is not Collection))
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }
      if (obj is not Collection) // POC: equivalent of converter.CanConvertToNative(obj) ?
      {
        objectsToConvert.Add((objPath, obj));
      }
    }

    return objectsToConvert;
  }

  private void ConvertObjects(Base commitObject, ReceiverModelCard modelCard, CancellationTokenSource cts)
  {
    // POC: Progress here
    Commands.SetModelProgress(modelCard.ModelCardId, new ModelCardProgress() { Status = "Converting" });

    ISpeckleConverterToHost converter = _speckleConverterToHostFactory.ResolveScopedInstance();

    // Layer filter for received commit with project and model name
    _autocadLayerManager.CreateLayerFilter(modelCard.ProjectName, modelCard.ModelName);
    var objectsWithPath = GetBaseWithPath(commitObject, cts);
    var baseLayerPrefix = $"SPK-{modelCard.ProjectName}-{modelCard.ModelName}-";

    var uniqueLayerNames = new HashSet<string>();
    var handleValues = new List<string>();
    var count = 0;

    foreach (var (path, obj) in objectsWithPath)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      try
      {
        var layerFullName = _autocadLayerManager.LayerFullName(baseLayerPrefix, string.Join("-", path));

        if (uniqueLayerNames.Add(layerFullName))
        {
          _autocadLayerManager.CreateLayerOrPurge(layerFullName);
        }

        var converted = converter.Convert(obj);
        var flattened = FlattenToNativeConversionResult(converted);
        foreach (Entity conversionResult in flattened.Cast<Entity>())
        {
          if (conversionResult == null)
          {
            continue;
          }

          conversionResult.Append(layerFullName);
          handleValues.Add(conversionResult.Handle.Value.ToString());
        }

        Commands.SetModelProgress(
          modelCard.ModelCardId,
          new ModelCardProgress() { Status = "Converting", Progress = (double)++count / objectsWithPath.Count }
        );
      }
      catch (Exception e) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        // POC: report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }
  }

  /// <summary>
  /// Utility function to flatten a conversion result that might have nested lists of objects.
  /// This happens, for example, in the case of multiple display value fallbacks for a given object.
  /// </summary>
  /// <param name="item"></param>
  /// <returns></returns>
  private List<object> FlattenToNativeConversionResult(object item)
  {
    var convertedList = new List<object>();
    void Flatten(object item)
    {
      if (item is IList list)
      {
        foreach (object child in list)
        {
          Flatten(child);
        }
      }
      else
      {
        convertedList.Add(item);
      }
    }
    Flatten(item);
    return convertedList;
  }

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public void Dispose()
  {
    IsDisposed = true;
    _speckleConverterToHostFactory.Dispose();
  }

  public bool IsDisposed { get; private set; }

  private static readonly string[] s_separator = new[] { "\\" };
}

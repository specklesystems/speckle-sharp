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
using Speckle.Core.Models.Extensions;
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
    Parent.RunOnMainThread(async () => await ReceiveInternal(modelCardId).ConfigureAwait(false));
    return Task.CompletedTask;
  }

  private async Task ReceiveInternal(string modelCardId)
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
        await apiClient.CommitGet(modelCard.ProjectId, modelCard.SelectedVersionId, cts.Token).ConfigureAwait(false)
        ?? throw new SpeckleException($"Failed to receive commit: {modelCard.SelectedVersionId} from server)");

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
      List<string> convertedObjectIds = ConvertObjects(commitObject, modelCard, cts);

      Commands.SetModelReceiveResult(modelCardId, convertedObjectIds);
    }
    catch (OperationCanceledException)
    {
      // POC: not sure here need to handle anything. UI already aware it cancelled operation visually.
      return;
    }
    catch (Exception e) when (!e.IsFatal()) // All exceptions should be handled here if possible, otherwise we enter "crashing the host app" territory.
    {
      Commands.SetModelError(modelCardId, e);
    }
  }

  private List<(List<string>, Base)> GetBaseWithPath(Base commitObject, CancellationTokenSource cts)
  {
    List<(List<string>, Base)> objectsToConvert = new();
    foreach ((List<string> objPath, Base obj) in commitObject.TraverseWithPath((obj) => obj is not Collection))
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

  private List<string> ConvertObjects(Base commitObject, ReceiverModelCard modelCard, CancellationTokenSource cts)
  {
    // Prompt the UI conversion started. Progress bar will swoosh.
    Commands.SetModelProgress(modelCard.ModelCardId, new ModelCardProgress() { Status = "Converting" });

    ISpeckleConverterToHost converter = _speckleConverterToHostFactory.ResolveScopedInstance();

    // Layer filter for received commit with project and model name
    _autocadLayerManager.CreateLayerFilter(modelCard.ProjectName, modelCard.ModelName);
    List<(List<string>, Base)> objectsWithPath = GetBaseWithPath(commitObject, cts);
    string baseLayerPrefix = $"SPK-{modelCard.ProjectName}-{modelCard.ModelName}-";

    HashSet<string> uniqueLayerNames = new();
    List<string> handleValues = new();
    int count = 0;

    using (TransactionContext.StartTransaction(Application.DocumentManager.MdiActiveDocument))
    {
      foreach ((List<string> path, Base obj) in objectsWithPath)
      {
        if (cts.IsCancellationRequested)
        {
          throw new OperationCanceledException(cts.Token);
        }

        try
        {
          string layerFullName = _autocadLayerManager.LayerFullName(baseLayerPrefix, string.Join("-", path));

          if (uniqueLayerNames.Add(layerFullName))
          {
            _autocadLayerManager.CreateLayerOrPurge(layerFullName);
          }

          object converted = converter.Convert(obj);
          List<object> flattened = Core.Models.Utilities.FlattenToNativeConversionResult(converted);

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
        catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
        {
          // POC: report, etc.
          Debug.WriteLine("conversion error happened.");
        }
      }
    }
    return handleValues;
  }

  public void CancelSend(string modelCardId) => _cancellationManager.CancelOperation(modelCardId);

  public void Dispose()
  {
    IsDisposed = true;
    _speckleConverterToHostFactory.Dispose();
  }

  public bool IsDisposed { get; private set; }
}

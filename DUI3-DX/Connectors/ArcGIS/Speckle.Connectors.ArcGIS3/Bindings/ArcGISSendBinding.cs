using System.Diagnostics.CodeAnalysis;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.ArcGIS.Filters;
using Speckle.Connectors.ArcGis.Operations.Send;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using ICancelable = System.Reactive.Disposables.ICancelable;
using Speckle.Connectors.ArcGIS.Utils;
using Speckle.Connectors.DUI.Models.Card.SendFilter;

namespace Speckle.Connectors.ArcGIS.Bindings;

public sealed class ArcGISSendBinding : ISendBinding, ICancelable
{
  public string Name => "sendBinding";
  public SendBindingUICommands Commands { get; }
  public IBridge Parent { get; }

  private readonly ArcGISDocumentStore _store;
  private readonly IScopedFactory<ISpeckleConverterToSpeckle> _speckleConverterToSpeckleFactory;
  private readonly CancellationManager _cancellationManager;
  private readonly SendOperation _sendOperation;

  /// <summary>
  /// Used internally to aggregate the changed objects' id.
  /// </summary>
  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public ArcGISSendBinding(
    ArcGISDocumentStore store,
    IBridge parent,
    IScopedFactory<ISpeckleConverterToSpeckle> speckleConverterToSpeckleFactory,
    SendOperation sendOperation,
    CancellationManager cancellationManager
  )
  {
    _store = store;
    _speckleConverterToSpeckleFactory = speckleConverterToSpeckleFactory;
    _sendOperation = sendOperation;
    _cancellationManager = cancellationManager;

    Parent = parent;
    Commands = new SendBindingUICommands(parent);
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter> { new ArcGISSelectionFilter { IsDefault = true } };
  }

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
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = _cancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model
      if (_store.GetModelById(modelCardId) is not SenderModelCard modelCard)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }

      string versionId = await _sendOperation
        .Execute(
          //modelCard.SendFilter,
          modelCard.AccountId,
          modelCard.ProjectId,
          modelCard.ModelId,
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

    Commands.SetModelsExpired(expiredSenderIds);
    ChangedObjectIds = new HashSet<string>();
  }

  private void OnSendOperationProgress(string modelCardId, string status, double? progress)
  {
    Commands.SetModelProgress(modelCardId, new ModelCardProgress { Status = status, Progress = progress });
  }

  public void Dispose()
  {
    IsDisposed = true;
    _speckleConverterToSpeckleFactory.Dispose();
  }

  public bool IsDisposed { get; private set; }
}

using Speckle.Connectors.DUI.Models.Card.SendFilter;

namespace Speckle.Connectors.DUI.Bindings;

public interface ISendBinding : IBinding
{
  public List<ISendFilter> GetSendFilters();

  /// <summary>
  /// Instructs the host app to start sending this model.
  /// </summary>
  /// <param name="modelCardId"></param>
  public Task Send(string modelCardId, CancellationToken ct);

  /// <summary>
  /// Instructs the host app to  cancel the sending for a given model.
  /// </summary>
  /// <param name="modelCardId"></param>
  public void CancelSend(string modelCardId);

  public SendBindingUICommands Commands { get; }
}

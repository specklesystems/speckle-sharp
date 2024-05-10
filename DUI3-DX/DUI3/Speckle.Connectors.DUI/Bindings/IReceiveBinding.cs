namespace Speckle.Connectors.DUI.Bindings;

public interface IReceiveBinding : IBinding
{
  /// <summary>
  /// Instructs the host app to start receiving this model version.
  /// </summary>
  /// <param name="modelCardId"> Model card id</param>
  public Task Receive(string modelCardId);

  /// <summary>
  /// Instructs the host app to  cancel the receiving for a given model.
  /// </summary>
  /// <param name="modelCardId"></param>
  public void CancelReceive(string modelCardId);
}

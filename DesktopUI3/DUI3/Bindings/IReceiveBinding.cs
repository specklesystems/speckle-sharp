using DUI3.Models;

namespace DUI3.Bindings
{
  public interface IReceiveBinding : IBinding
  {
    /// <summary>
    /// Instructs the host app to start receiving this model version.
    /// </summary>
    /// <param name="modelCardId"> Model card id</param>
    /// <param name="versionId"> Version id to receive</param>
    public void Receive(string modelCardId, string versionId);

    /// <summary>
    /// Instructs the host app to  cancel the receiving for a given model.
    /// </summary>
    /// <param name="modelCardId"></param>
    public void CancelReceive(string modelCardId);
  }

  public static class ReceiveBindingEvents
  {
    public static readonly string ReceiversExpired = "receiversExpired";
    public static readonly string ReceiverProgress = "receiverProgress";
  }

  public class ReceiverModelCard : ModelCard
  {
  }

  public class ReceiverProgress
  {
    public string Id { get; set; }
    public string Status { get; set; }
    public double Progress { get; set; }
  }
}

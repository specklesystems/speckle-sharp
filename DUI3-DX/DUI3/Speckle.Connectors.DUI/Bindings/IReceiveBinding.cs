using Speckle.Connectors.DUI.Models.Card;
using System.Threading.Tasks;

namespace Speckle.Connectors.DUI.Bindings;

public interface IReceiveBinding : IBinding
{
  /// <summary>
  /// Instructs the host app to start receiving this model version.
  /// </summary>
  /// <param name="modelCardId"> Model card id</param>
  /// <param name="versionId"> Version id to receive</param>
  public Task Receive(string modelCardId, string versionId);

  /// <summary>
  /// Instructs the host app to  cancel the receiving for a given model.
  /// </summary>
  /// <param name="modelCardId"></param>
  public void CancelReceive(string modelCardId);
}

public static class ReceiveBindingEvents
{
  public const string RECEIVERS_EXPIRED = "receiversExpired";
  public const string RECEIVERS_PROGRESS = "receiverProgress";
  public const string NOTIFY = "notify";
}

public class ReceiverModelCard : ModelCard { }

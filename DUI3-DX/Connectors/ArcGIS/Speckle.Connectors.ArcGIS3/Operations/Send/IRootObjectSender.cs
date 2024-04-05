using Speckle.Core.Models;

namespace Speckle.Connectors.ArcGis.Operations.Send;

//poc: duped from rhino

/// <summary>
/// Contract for the send operation that handles an assembled <see cref="Base"/> object.
/// In production, this will send to a server.
/// In testing, this could send to a sqlite db or just save to a dictionary.
/// </summary>
public interface IRootObjectSender
{
  public Task<string> Send(
    Base commitObject,
    string accountId,
    string projectId,
    string modelId,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  );
}

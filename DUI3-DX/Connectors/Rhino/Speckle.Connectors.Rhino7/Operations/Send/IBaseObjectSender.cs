using System;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace Speckle.Connectors.Rhino7.Operations.Send;

public interface IBaseObjectSender
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

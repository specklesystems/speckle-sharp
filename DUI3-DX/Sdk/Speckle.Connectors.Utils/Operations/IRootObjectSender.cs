using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace Speckle.Connectors.Utils.Operations;

/// <summary>
/// Contract for the send operation that handles an assembled <see cref="Base"/> object.
/// In production, this will send to a server.
/// In testing, this could send to a sqlite db or just save to a dictionary.
/// </summary>
public interface IRootObjectSender
{
  public Task<(string rootObjId, Dictionary<string, ObjectReference> convertedReferences)> Send(
    Base commitObject,
    SendInfo sendInfo,
    Action<string, double?>? onOperationProgressed = null,
    CancellationToken ct = default
  );
}

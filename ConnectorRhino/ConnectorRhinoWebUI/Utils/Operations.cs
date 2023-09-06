using System;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace ConnectorRhinoWebUI.Utils;

public static class Operations
{
 
  /// <summary>
  /// Convenience wrapper around <see cref="Receive"/> with connector-style error handling
  /// </summary>
  /// <param name="commit">the <see cref="Commit"/> to receive</param>
  /// <returns>The requested commit data</returns>
  /// <exception cref="SpeckleException">Thrown when any receive operation errors</exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="progress"/> requests a cancellation</exception>
  public static async Task<Base> ReceiveCommit(Account account, string projectId, string referencedObjectId)
  {
    using ServerTransport transport = new(account, projectId);

    Base? commitObject = await Speckle.Core.Api.Operations
      .Receive(
        referencedObjectId,
        transport,
        onErrorAction: (s, ex) =>
        {
          //Don't wrap cancellation exceptions!
          if (ex is OperationCanceledException)
            throw ex;

          //Treat all operation errors as fatal
          throw new SpeckleException($"Failed to receive commit: {referencedObjectId} objects from server: {s}", ex);
        },
        disposeTransports: false
      )
      .ConfigureAwait(false);

    if (commitObject == null)
      throw new SpeckleException(
        $"Failed to receive commit: {referencedObjectId} objects from server: {nameof(Speckle.Core.Api.Operations)} returned null"
      );

    return commitObject;
  }
}

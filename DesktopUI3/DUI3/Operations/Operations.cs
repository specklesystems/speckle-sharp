using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DUI3.Bindings;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace DUI3.Operations;

public static class Operations
{
  public static async Task<string> Send(IBridge bridge, string modelCardId, Base commitObject, CancellationToken token, List<ITransport> transports)
  {
    // TODO: Fix send operations haven't succeeded
    // Pass null progress value to let UI swooshing progress bar
    Progress.SerializerProgressToBrowser(bridge, modelCardId, null);
    var objectId = await Speckle.Core.Api.Operations.Send(
        commitObject,
        token,
        transports,
        disposeTransports: true
      )
      .ConfigureAwait(true);
    // Pass 1 progress value to let UI finish progress
    Progress.SerializerProgressToBrowser(bridge, modelCardId, 1);
    return objectId;
  }

  public static void CreateVersion(IBridge bridge, SenderModelCard model, string objectId, string hostAppName)
  {
    bridge.SendToBrowser(
      SendBindingEvents.CreateVersion,
      new CreateVersion()
      {
        AccountId = model.AccountId,
        ModelId = model.ModelId,
        ModelCardId = model.Id,
        ProjectId = model.ProjectId,
        ObjectId = objectId,
        Message = "Test",
        SourceApplication = hostAppName
      });
  }
  
  public static async Task<Base> GetCommitBase(IBridge parent, ReceiverModelCard modelCard, string versionId, CancellationToken token)
  {
    // Pass null progress value to let UI swooshing progress bar
    Progress.DeserializerProgressToBrowser(parent, modelCard.Id, null);

    Account account = Accounts.GetAccount(modelCard.AccountId);
    Client client = new(account);
      
    Commit version = await client.CommitGet(token, modelCard.ProjectId, versionId).ConfigureAwait(false);

    Base commitObject = await ReceiveCommit(
      account,
      modelCard.ProjectId,
      version.referencedObject,
      token).ConfigureAwait(true);
    
    // Pass 1 progress value to let UI finish progress
    Progress.DeserializerProgressToBrowser(parent, modelCard.Id, 1);
    
    return commitObject;
  }
  
  
  /// <summary>
  /// Convenience wrapper around <see cref="Receive"/> with connector-style error handling
  /// </summary>
  /// <param name="commit">the <see cref="Commit"/> to receive</param>
  /// <returns>The requested commit data</returns>
  /// <exception cref="SpeckleException">Thrown when any receive operation errors</exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="progress"/> requests a cancellation</exception>
  private static async Task<Base> ReceiveCommit(Account account, string projectId, string referencedObjectId, CancellationToken token)
  {
    using ServerTransport transport = new(account, projectId);

    Base? commitObject = await Speckle.Core.Api.Operations
      .Receive(
        referencedObjectId,
        token,
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

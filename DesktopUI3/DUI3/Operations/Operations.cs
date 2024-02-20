﻿#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DUI3.Bindings;
using DUI3.Models.Card;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace DUI3.Operations;

public static class Operations
{
  public static async Task<Base> GetCommitBase(
    IBridge parent,
    ReceiverModelCard modelCard,
    CancellationToken token
  )
  {
    Account account = Accounts.GetAccount(modelCard.AccountId);
    Client client = new(account);

    Commit version = await client.CommitGet(modelCard.ProjectId, modelCard.SelectedVersionId, token).ConfigureAwait(false);

    Base commitObject = await ReceiveCommit(account, modelCard.ProjectId, version.referencedObject, parent, token)
      .ConfigureAwait(true);
    
    client.Dispose();
    return commitObject;
  }

  /// <summary>
  /// Convenience wrapper around <see cref="Receive"/> with connector-style error handling
  /// </summary>
  /// <param name="commit">the <see cref="Commit"/> to receive</param>
  /// <returns>The requested commit data</returns>
  /// <exception cref="SpeckleException">Thrown when any receive operation errors</exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="progress"/> requests a cancellation</exception>
  private static async Task<Base> ReceiveCommit(
    Account account,
    string projectId,
    string referencedObjectId,
    IBridge parent,
    CancellationToken token
  )
  {
    using ServerTransport transport = new(account, projectId);

    Base? commitObject =
      await Speckle.Core.Api.Operations
        .Receive(
          referencedObjectId,
          token,
          transport,
          onErrorAction: (s, ex) =>
          {
            //Don't wrap cancellation exceptions!
            if (ex is OperationCanceledException)
            {
              throw ex;
            }

            //Treat all operation errors as fatal
            throw new SpeckleException($"Failed to receive commit: {referencedObjectId} objects from server: {s}", ex);
          },
          onProgressAction: (ConcurrentDictionary<string, int> dict) =>
          {
            // TODO: Progress report? 
            // ReceiveBindingUiCommands.SetModelProgress(new ModelCardProgress() { Status = @"Receiving from server {}"});
          },
          disposeTransports: false
        )
        .ConfigureAwait(false)
      ?? throw new SpeckleException(
        $"Failed to receive commit: {referencedObjectId} objects from server: {nameof(Speckle.Core.Api.Operations)} returned null"
      );
    return commitObject;
  }
}

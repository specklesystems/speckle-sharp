#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Controls.StreamEditControls;
using Serilog.Events;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace DesktopUI2;

/// <summary>
/// Code shared between connector bindings.
/// </summary>
public static class ConnectorHelpers
{
  /// <summary>
  /// Proxy <see cref="StreamState"/> commit ID for getting the latest commit on a branch
  /// </summary>
  public const string LatestCommitString = "latest";

  /// <summary>
  /// Convenience wrapper around <see cref="Receive"/> with connector-style error handling
  /// </summary>
  /// <param name="commit">the <see cref="Commit"/> to receive</param>
  /// <param name="state">Current Stream card state (does not mutate)</param>
  /// <param name="progress">View model to update with progress</param>
  /// <returns>The requested commit data</returns>
  /// <exception cref="SpeckleException">Thrown when any receive operation errors</exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="progress"/> requests a cancellation</exception>
  public static async Task<Base> ReceiveCommit(Commit commit, StreamState state, ProgressViewModel progress)
  {
    progress.CancellationToken.ThrowIfCancellationRequested();

    using ServerTransport transport = new(state.Client.Account, state.StreamId);

    Base commitObject = await Operations
      .Receive(
        commit.referencedObject,
        transport,
        onProgressAction: dict => progress.Update(dict),
        onTotalChildrenCountKnown: c => progress.Max = c,
        cancellationToken: progress.CancellationToken
      )
      .ConfigureAwait(false);

    return commitObject;
  }

  /// <param name="cancellationToken">Progress cancellation token</param>
  /// <param name="state">Current Stream card state (does not mutate)</param>
  /// <returns>Requested Commit</returns>
  /// <exception cref="SpeckleException">Thrown when any client errors</exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> requests a cancellation</exception>
  public static async Task<Commit> GetCommitFromState(StreamState state, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    Commit commit;
    try
    {
      if (state.CommitId == LatestCommitString) //if "latest", always make sure we get the latest commit
      {
        var res = await state.Client
          .BranchGet(state.StreamId, state.BranchName, 1, cancellationToken)
          .ConfigureAwait(false);
        commit = res.commits.items.First();
      }
      else
      {
        var res = await state.Client.CommitGet(state.StreamId, state.CommitId, cancellationToken).ConfigureAwait(false);
        commit = res;
      }
    }
    catch (OperationCanceledException)
    {
      //Don't wrap cancellation exceptions
      throw;
    }
    catch (Exception ex)
    {
      throw new SpeckleException(
        $"Failed to fetch requested commit id: {state.CommitId} from branch: \"{state.BranchName}\" from stream: {state.StreamId}",
        ex
      );
    }

    return commit;
  }

  /// <summary>
  /// Try catch wrapper around <see cref="Client.CommitReceived(CommitReceivedInput, CancellationToken)"/> with logging
  /// </summary>
  public static async Task TryCommitReceived(
    Client client,
    CommitReceivedInput commitReceivedInput,
    CancellationToken cancellationToken = default
  )
  {
    try
    {
      await client.CommitReceived(commitReceivedInput, cancellationToken).ConfigureAwait(false);
    }
    catch (SpeckleException ex)
    {
      SpeckleLog.Logger
        .ForContext("commitReceivedInput", commitReceivedInput)
        .Warning(ex, "Client operation {operationName} failed", nameof(Client.CommitReceived));
    }
  }

  /// <inheritdoc cref="TryCommitReceived(CancellationToken, Client, CommitReceivedInput, LogEventLevel)"/>
  public static async Task TryCommitReceived(
    StreamState state,
    Commit commit,
    string sourceApplication,
    CancellationToken cancellationToken = default
  )
  {
    cancellationToken.ThrowIfCancellationRequested();
    var commitReceivedInput = new CommitReceivedInput
    {
      streamId = state.StreamId,
      commitId = commit.id,
      message = commit.message,
      sourceApplication = sourceApplication
    };
    await TryCommitReceived(state.Client, commitReceivedInput, cancellationToken).ConfigureAwait(false);
  }

  //TODO: should this just be how `CommitCreate` id implemented?
  /// <summary>
  /// Wrapper around <see cref="Client.CommitCreate(CommitCreateInput, CancellationToken)"/> with Error handling.
  /// </summary>
  /// <inheritdoc cref="Client.CommitCreate(CommitCreateInput, CancellationToken)"/>
  /// <exception cref="OperationCanceledException"></exception>
  /// <exception cref="SpeckleException">All other exceptions</exception>
  public static async Task<string> CreateCommit(
    Client client,
    CommitCreateInput commitInput,
    CancellationToken cancellationToken = default
  )
  {
    try
    {
      var commitId = await client.CommitCreate(commitInput, cancellationToken).ConfigureAwait(false);
      return commitId;
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger
        .ForContext("commitInput", commitInput)
        .Warning(ex, "Client operation {operationName} failed", nameof(Client.CommitCreate));
      throw new SpeckleException("Failed to create commit object", ex);
    }
  }

  /// <exception cref="OperationCanceledException"></exception>
  /// <exception cref="SpeckleException"></exception>
  [Obsolete(nameof(Operations.Send) + "No longer accepts OnErrorHandler")]
  public static void DefaultSendErrorHandler(string error, Exception ex)
  {
    //Don't wrap cancellation exceptions!
    if (ex is OperationCanceledException cex)
    {
      throw cex;
    }

    //Treat all operation errors as fatal
    throw new SpeckleException($"Failed to send objects to server - {error}", ex);
  }

  public const string ConversionFailedLogTemplate = "Converter failed to convert object";

  public static void LogConversionException(Exception ex)
  {
    LogEventLevel logLevel = ex switch
    {
      ConversionNotSupportedException => LogEventLevel.Verbose,
      ConversionSkippedException => LogEventLevel.Verbose, //Deprecated
      ConversionException => LogEventLevel.Warning,
      _ => LogEventLevel.Error
    };

    SpeckleLog.Logger.Write(logLevel, ex, ConversionFailedLogTemplate);
  }

  public static ApplicationObject.State GetAppObjectFailureState(Exception ex)
  {
    if (ex is null)
    {
      throw new ArgumentNullException(nameof(ex));
    }

    return ex switch
    {
      ConversionNotSupportedException => ApplicationObject.State.Skipped,
      ConversionSkippedException => ApplicationObject.State.Skipped, //Deprecated
      _ => ApplicationObject.State.Failed,
    };
  }

  #region deprecated members

  [Obsolete("Use overload that has cancellation token last", true)]
  public static async Task TryCommitReceived(
    CancellationToken cancellationToken,
    Client client,
    CommitReceivedInput commitReceivedInput
  )
  {
    await TryCommitReceived(client, commitReceivedInput, cancellationToken).ConfigureAwait(false);
  }

  [Obsolete("Use overload that has cancellation token last", true)]
  public static async Task<Commit> GetCommitFromState(CancellationToken cancellationToken, StreamState state)
  {
    return await GetCommitFromState(state, cancellationToken).ConfigureAwait(false);
  }

  [Obsolete("Use overload that has cancellation token last", true)]
  public static async Task TryCommitReceived(
    CancellationToken cancellationToken,
    StreamState state,
    Commit commit,
    string sourceApplication
  )
  {
    await TryCommitReceived(state, commit, sourceApplication, cancellationToken).ConfigureAwait(false);
  }

  [Obsolete("Use overload that has cancellation token last", true)]
  public static async Task<string> CreateCommit(
    CancellationToken cancellationToken,
    Client client,
    CommitCreateInput commitInput
  )
  {
    return await CreateCommit(client, commitInput, cancellationToken).ConfigureAwait(false);
  }
  #endregion
}

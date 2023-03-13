#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Serilog;
using Serilog.Events;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace DesktopUI2
{
    
    /// <summary>
    /// Code shared between connector bindings.
    /// </summary>
    public static class ConnectorHelpers
    {
        /// <summary>
        /// Proxy <see cref="StreamState"/> commit ID for getting the latest commit on a branch
        /// </summary>
        public const string LatestCommitString = "latest";
        
        /// <param name="commit">the <see cref="Commit"/> to receive</param>
        /// <param name="state"></param>
        /// <param name="progress"></param>
        /// <returns>The requested commit data</returns>
        /// <exception cref="SpeckleException">Thrown when any receive operation errors</exception>
        /// <exception cref="OperationCanceledException">Thrown when <paramref name="progress"/> requests a cancellation</exception>
        public static async Task<Base> ReceiveCommit(Commit commit, StreamState state, ProgressViewModel progress)
        {
            progress.CancellationToken.ThrowIfCancellationRequested();

            var transport = new ServerTransport(state.Client.Account, state.StreamId);
      
            Base? commitObject = await Operations.Receive(
                commit.referencedObject,
                progress.CancellationToken,
                transport,
                onProgressAction: dict => progress.Update(dict),
                onErrorAction: (s, ex) =>
                {
                    //Don't wrap cancellation exceptions!      
                    if (ex is OperationCanceledException) throw ex;
          
                    //HACK: Sometimes, the task was cancelled, and Operations.Receive doesn't fail in a reliable way. In this case, the exception is often simply a symptom of a cancel.
                    if (progress.CancellationToken.IsCancellationRequested)
                    {
                        Log.Warning(ex, "A task was cancelled, ignoring potentially symptomatic exception");
                        progress.CancellationToken.ThrowIfCancellationRequested();
                    }
          
                    //Treat all operation errors as fatal
                    throw new SpeckleException($"Failed to receive commit: {commit.id} objects from server", ex);
                },
                onTotalChildrenCountKnown: (c) => progress.Max = c,
                disposeTransports: true
            );

            if (commitObject == null)
                throw new SpeckleException($"Failed to receive commit: {commit.id} objects from server: {nameof(Operations.Receive)} returned null");
            
            return commitObject;
        }
        
        
#nullable disable
        
        /// <param name="cancellationToken">Progress cancellation token</param>
        /// <param name="state">Current Stream card state</param>
        /// <returns>Requested Commit</returns>
        /// <exception cref="SpeckleException">Thrown when any client errors</exception>
        /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> requests a cancellation</exception>
        public static async Task<Commit> GetCommitFromState(CancellationToken cancellationToken, StreamState state)
        {
            cancellationToken.ThrowIfCancellationRequested();
      
            Commit commit;
            try
            {
                if (state.CommitId == LatestCommitString) //if "latest", always make sure we get the latest commit
                {
                    var res = await state.Client.BranchGet(cancellationToken, state.StreamId, state.BranchName, 1);
                    commit = res.commits.items.First();
                }
                else
                {
                    var res = await state.Client.CommitGet(cancellationToken, state.StreamId, state.CommitId);
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
                throw new SpeckleException($"Failed to fetch requested commit id: {state.CommitId} from branch: \"{state.BranchName}\" from stream: {state.StreamId}", ex);
            }

            return commit;
        }


        public const LogEventLevel DefaultTryCommitReceivedLogLevel = LogEventLevel.Warning;
        
        /// <summary>
        /// Try catch wrapper around <see cref="Client.CommitReceived(CancellationToken, CommitReceivedInput)"/> with logging
        /// </summary>
        public static async Task TryCommitReceived(CancellationToken cancellationToken,
            Client client,
            CommitReceivedInput commitReceivedInput,
            LogEventLevel logLevel = DefaultTryCommitReceivedLogLevel)
        {
            try
            {
                await client.CommitReceived(cancellationToken, commitReceivedInput);
            }
            catch(SpeckleException ex)
            {
                Log.ForContext("commitReceivedInput", commitReceivedInput)
                    .Write(logLevel, ex, "Client operation {operationName} failed", nameof(Client.CommitReceived));
            }
        }
        
        /// <inheritdoc cref="TryCommitReceived(CancellationToken, Client, CommitReceivedInput, LogEventLevel)"/>
        public static async Task TryCommitReceived(CancellationToken cancellationToken,
            StreamState state,
            Commit commit,
            string sourceApplication,
            LogEventLevel logLevel = DefaultTryCommitReceivedLogLevel)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var commitReceivedInput = new CommitReceivedInput
            {
                streamId = state.StreamId,
                commitId = commit.id,
                message = commit.message,
                sourceApplication = sourceApplication
            };
            await TryCommitReceived(cancellationToken, state.Client, commitReceivedInput, logLevel);
        }
    }
}
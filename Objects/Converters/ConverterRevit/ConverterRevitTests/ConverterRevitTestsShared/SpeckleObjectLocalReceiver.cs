using System;
using System.Threading;
using System.Threading.Tasks;
using ConnectorRevit;
using ConverterRevitTests;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConverterRevitTestsShared
{
  internal sealed class SpeckleObjectLocalReceiver : ISpeckleObjectReceiver
  {
    private SpeckleConversionFixture fixture;

    public SpeckleObjectLocalReceiver(SpeckleConversionFixture fixture)
    {
      this.fixture = fixture;
    }

    public async Task<Commit> GetCommitFromState(StreamState state, CancellationToken cancellationToken)
    {
      return fixture.Commits[state.CommitId];
    }

    public async Task<Base> ReceiveCommit(Commit commit, StreamState state, ProgressViewModel progress)
    {
      Base? commitObject = await Operations
        .Receive(
          commit.referencedObject,
          progress.CancellationToken,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: (s, ex) =>
          {
            //Don't wrap cancellation exceptions!
            if (ex is OperationCanceledException)
              throw ex;

            //HACK: Sometimes, the task was cancelled, and Operations.Receive doesn't fail in a reliable way. In this case, the exception is often simply a symptom of a cancel.
            if (progress.CancellationToken.IsCancellationRequested)
            {
              progress.CancellationToken.ThrowIfCancellationRequested();
            }

            //Treat all operation errors as fatal
            throw new SpeckleException($"Failed to receive commit: {commit.id} objects from server: {s}", ex);
          },
          onTotalChildrenCountKnown: c => progress.Max = c
        )
        .ConfigureAwait(false);

      if (commitObject == null)
        throw new SpeckleException(
          $"Failed to receive commit: {commit.id} objects from server: {nameof(Operations.Receive)} returned null"
        );

      return commitObject;
    }

    public async Task TryCommitReceived(StreamState state, Commit myCommit, string revitAppName, CancellationToken cancellationToken)
    {
      // do nothing here
    }
  }
}

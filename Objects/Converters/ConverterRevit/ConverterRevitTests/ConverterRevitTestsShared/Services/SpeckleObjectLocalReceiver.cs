using System;
using System.Threading.Tasks;
using ConnectorRevit.Services;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using RevitSharedResources.Interfaces;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace ConverterRevitTestsShared.Services
{
  internal class SpeckleObjectLocalReceiver : ISpeckleObjectReceiver
  {
    private StreamState streamState;
    public SpeckleObjectLocalReceiver(IEntityProvider<StreamState> streamStateProvider) 
    {
      this.streamState = streamStateProvider.Entity;
    }
    public async Task<Base> ReceiveCommitObject(StreamState state, ProgressViewModel progress)
    {
      Base? commitObject = await Operations
        .Receive(
          streamState.ReferencedObject,
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
            throw new SpeckleException($"Failed to receive commit: {streamState.ReferencedObject} objects from server: {s}", ex);
          },
          onTotalChildrenCountKnown: c => progress.Max = c
        )
        .ConfigureAwait(false);

      if (commitObject == null)
        throw new SpeckleException(
          $"Failed to receive commit: {streamState.ReferencedObject} objects from server: {nameof(Operations.Receive)} returned null"
        );

      return commitObject;
    }
  }
}

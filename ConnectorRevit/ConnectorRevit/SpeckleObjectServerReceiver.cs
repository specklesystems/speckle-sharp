using System.Threading;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace ConnectorRevit
{
  internal sealed class SpeckleObjectServerReceiver : ISpeckleObjectReceiver
  {
    public async Task<Commit> GetCommitFromState(StreamState state, CancellationToken token)
    {
      return await ConnectorHelpers.GetCommitFromState(state, token).ConfigureAwait(false);
    }

    public async Task<Base> ReceiveCommit(Commit myCommit, StreamState state, ProgressViewModel progress)
    {
      return await ConnectorHelpers.ReceiveCommit(myCommit, state, progress).ConfigureAwait(false);
    }

    public async Task TryCommitReceived(StreamState state, Commit myCommit, string revitAppName, CancellationToken cancellationToken)
    {
      await ConnectorHelpers.TryCommitReceived(state, myCommit, revitAppName, cancellationToken).ConfigureAwait(false);
    }
  }
}

using System.Threading;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models.Interfaces;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace ConnectorRevit
{
  internal sealed class CommitReceiver : ISpeckleObjectReceiver
  {
    public async Task<Commit> GetCommitFromState(IStreamState state, CancellationToken token)
    {
      return await ConnectorHelpers.GetCommitFromState(state, token).ConfigureAwait(false);
    }

    public async Task<Base> ReceiveCommit(Commit myCommit, IStreamState state, ProgressViewModel progress)
    {
      return await ConnectorHelpers.ReceiveCommit(myCommit, state, progress);
    }

    public async Task TryCommitReceived(IStreamState state, Commit myCommit, string revitAppName, CancellationToken cancellationToken)
    {
      await ConnectorHelpers.TryCommitReceived(state, myCommit, revitAppName, cancellationToken);
    }
  }
}

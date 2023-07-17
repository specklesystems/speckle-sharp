using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorRevit;
using Speckle.Core.Models;

namespace ConnectorRevit.Services
{
  internal sealed class SpeckleObjectServerReceiver : ISpeckleObjectReceiver
  {
    public async Task<Base> ReceiveCommitObject(StreamState state, ProgressViewModel progress)
    {
      var myCommit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken)
        .ConfigureAwait(false);
      state.LastCommit = myCommit;
      var commitObject = await ConnectorHelpers.ReceiveCommit(myCommit, state, progress)
        .ConfigureAwait(false);
      await ConnectorHelpers.TryCommitReceived(state, myCommit, ConnectorRevitUtils.RevitAppName, progress.CancellationToken).ConfigureAwait(false);

      return commitObject;
    }
  }
}

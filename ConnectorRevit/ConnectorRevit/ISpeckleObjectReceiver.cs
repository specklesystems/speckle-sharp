using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace ConnectorRevit
{
  public interface ISpeckleObjectReceiver
  {
    Task<Commit> GetCommitFromState(StreamState state, CancellationToken cancellationToken);
    Task<Base> ReceiveCommit(Commit myCommit, StreamState state, ProgressViewModel progress);
    Task TryCommitReceived(StreamState state, Commit myCommit, string revitAppName, CancellationToken cancellationToken);
  }
}

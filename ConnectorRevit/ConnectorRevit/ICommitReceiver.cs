using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models.Interfaces;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace ConnectorRevit
{
  public interface ISpeckleObjectReceiver
  {
    Task<Commit> GetCommitFromState(IStreamState state, CancellationToken cancellationToken);
    Task<Base> ReceiveCommit(Commit myCommit, IStreamState state, ProgressViewModel progress);
    Task TryCommitReceived(IStreamState state, Commit myCommit, string revitAppName, CancellationToken cancellationToken);
  }
}

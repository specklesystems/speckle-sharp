using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace ConnectorRevit
{
  public interface ISpeckleObjectSender
  {
    Task<string> CreateCommit(Client client, CommitCreateInput actualCommit, CancellationToken cancellationToken);
    Task<string> Send(Account account, string streamId, Base commitObject, ProgressViewModel progress);
  }
}

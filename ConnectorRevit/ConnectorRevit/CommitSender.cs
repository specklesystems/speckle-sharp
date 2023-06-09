using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorRevit;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace ConnectorRevit
{
  internal class CommitSender : ISpeckleObjectSender
  {
    public async Task<string> CreateCommit(Client client, CommitCreateInput actualCommit, CancellationToken cancellationToken)
    {
      return await ConnectorHelpers
        .CreateCommit(client, actualCommit, cancellationToken)
        .ConfigureAwait(false);
    }

    public async Task<string> Send(Account account, string streamId, Base commitObject, ProgressViewModel progress)
    {
      var transports = new List<ITransport>() { new ServerTransport(account, streamId) };

      return await Operations
        .Send(
          @object: commitObject,
          cancellationToken: progress.CancellationToken,
          transports: transports,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
          disposeTransports: true
        )
        .ConfigureAwait(true);
    }
  }
}

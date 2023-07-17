using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using RevitSharedResources.Interfaces;
using Speckle.ConnectorRevit;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace ConnectorRevit.Services
{
  internal class SpeckleObjectServerSender : ISpeckleObjectSender
  {
    private Client client;
    private StreamState state;
    private ProgressViewModel progress;
    public SpeckleObjectServerSender(
      Client client,
      IEntityProvider<StreamState> streamStateProvider,
      IEntityProvider<ProgressViewModel> progreeProvider
    )
    {
      this.client = client;
      this.state = streamStateProvider.Entity;
      this.progress = progreeProvider.Entity;
    }

    public string CommitId { get; set; }
    public string ObjectId { get; set; }

    public async Task<string> Send(
      string streamId, 
      string branchName, 
      string commitMessage, 
      Base commitObject, 
      int convertedCount
    )
    {
      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };
      ObjectId = await Speckle.Core.Api.Operations
        .Send(
          @object: commitObject,
          cancellationToken: progress.CancellationToken,
          transports: transports,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
          disposeTransports: true
        )
        .ConfigureAwait(true);

      progress.CancellationToken.ThrowIfCancellationRequested();

      var actualCommit = new CommitCreateInput()
      {
        streamId = streamId,
        objectId = ObjectId,
        branchName = branchName,
        message = commitMessage ?? $"Sent {convertedCount} objects from {ConnectorRevitUtils.RevitAppName}.",
        sourceApplication = ConnectorRevitUtils.RevitAppName,
      };

      if (state.PreviousCommitId != null)
      {
        actualCommit.parents = new List<string>() { state.PreviousCommitId };
      }
      CommitId = await ConnectorHelpers
        .CreateCommit(client, actualCommit, progress.CancellationToken)
        .ConfigureAwait(false);

      return CommitId;
    }
  }
}

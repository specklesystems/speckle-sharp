using System;
using System.Threading.Tasks;
using ConnectorRevit.Services;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using RevitSharedResources.Interfaces;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace ConverterRevitTestsShared.Services
{
  internal class SpeckleObjectLocalSender : ISpeckleObjectSender
  {
    private StreamState streamState;
    private ProgressViewModel progress;
    public SpeckleObjectLocalSender(
      IEntityProvider<StreamState> streamStateProvider,
      IEntityProvider<ProgressViewModel> progressProvider)
    {
      this.streamState = streamStateProvider.Entity;
      this.progress = progressProvider.Entity;
    }

    public string CommitId => "Dummy";

    public string ObjectId { get; set; }

    public async Task<string> Send(string streamId, string branchName, string commitMessage, Base commitObject, int convertedCount)
    {
      var objectId = await Operations.Send(@object: commitObject, cancellationToken: progress.CancellationToken)
        .ConfigureAwait(true);
      streamState.ReferencedObject = objectId;
      return objectId;
    }
  }
}

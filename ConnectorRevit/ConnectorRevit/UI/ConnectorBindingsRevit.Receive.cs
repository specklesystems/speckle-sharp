using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ConnectorRevit.Operations;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;

namespace Speckle.ConnectorRevit.UI
{

  public partial class ConnectorBindingsRevit
  {
    public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
    public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();

    public CancellationTokenSource CurrentOperationCancellation { get; set; }
    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    ///
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      CurrentOperationCancellation = progress.CancellationTokenSource;
      using var scope = Container.BeginLifetimeScope();

      // using objects such as the following DUI entity providers is a bad practice that we have to employ
      // to make up for not having proper DI configured in DUI
      var streamStateProvider = scope.Resolve<IEntityProvider<StreamState>>();
      streamStateProvider.Entity = state;
      var progressProvider = scope.Resolve<IEntityProvider<ProgressViewModel>>();
      progressProvider.Entity = progress;

      var receiveOperation = scope.Resolve<ReceiveOperation>();

      await receiveOperation.Receive().ConfigureAwait(false);

      CurrentOperationCancellation = null;
      return state;
    }
  }
}

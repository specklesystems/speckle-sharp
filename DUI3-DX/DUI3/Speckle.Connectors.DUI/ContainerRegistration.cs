using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Transports;

namespace Speckle.Connectors.DUI;

public static class ContainerRegistration
{
  public static void AddDUI(this SpeckleContainerBuilder speckleContainerBuilder)
  {
    // send operation and dependencies
    speckleContainerBuilder.AddSingletonInstance<ISyncToMainThread, SyncToUIThread>();
    speckleContainerBuilder.AddTransient<ITransport, ServerTransport>();
    speckleContainerBuilder.AddSingleton<IRootObjectSender, RootObjectSender>();
  }
}

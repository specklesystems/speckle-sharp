using Speckle.Autofac.DependencyInjection;

namespace Speckle.Connectors.DUI.WebView;

public static class ContainerRegistration
{
  public static void AddDUIView(this SpeckleContainerBuilder speckleContainerBuilder)
  {
    // send operation and dependencies
    speckleContainerBuilder.AddSingleton<DUI3ControlWebView>();
  }
}

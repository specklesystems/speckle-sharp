using ReactiveUI;
using Speckle.Core.Logging;

namespace DesktopUI2.ViewModels
{
  public class ShareViewModel : ReactiveObject
  {
    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string TitleFull => "Scheduler for " + Bindings.GetHostAppNameVersion();

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    public ShareViewModel()
    {

    }

    public ShareViewModel(ConnectorBindings _bindings)
    {
      Bindings = _bindings;
      Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());

    }


  }
}

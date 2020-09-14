using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Utils;
using Stylet;
using StyletIoC;

namespace Speckle.DesktopUI
{
  public class Bootstrapper : Bootstrapper<RootViewModel>
  {
    // TODO register connector bindings
    private ConnectorBindings bindings;

    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
      base.ConfigureIoC(builder);

      // Bind view model factory
      builder.Bind<IViewModelFactory>().ToAbstractFactory();

      builder.Bind<IStreamViewModelFactory>().ToAbstractFactory();
    }
  }
}
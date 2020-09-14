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

      // and factory for individual stream pages
      builder.Bind<IStreamViewModelFactory>().ToAbstractFactory();

      // and factory for dialog modals (eg create stream)
      builder.Bind<IDialogFactory>().ToAbstractFactory();
    }
  }
}

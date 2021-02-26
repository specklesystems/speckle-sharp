using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Logging;
using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Utils;
using Stylet;
using StyletIoC;

namespace Speckle.DesktopUI
{
  public class Bootstrapper : Bootstrapper<RootViewModel>
  {
    public ConnectorBindings Bindings = new DummyBindings();

    protected override void OnStart()
    {
      base.OnStart();
      Core.Logging.Setup.Init(Bindings.GetHostAppName());
      InitializeMaterialDesign();
      LoadThemeResources();
      Stylet.Logging.LogManager.Enabled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
      base.OnExit(e);
      Tracker.TrackPageview(Tracker.SESSION_END);
    }

    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
      base.ConfigureIoC(builder);

      // Bind view model factory
      builder.Bind<IViewModelFactory>().ToAbstractFactory();

      // and factory for individual stream pages
      builder.Bind<IStreamViewModelFactory>().ToAbstractFactory();

      // and factory for dialog modals (eg create stream)
      builder.Bind<IDialogFactory>().ToAbstractFactory();

      // and factory for repositories
      builder.Bind<StreamsRepository>().ToSelf();

      // and finally the external bindings (eg from Revit, Rhino, etc)
      builder.Bind<ConnectorBindings>().ToFactory(container =>
      {
        var bindings = Bindings;
        container.BuildUp(bindings); // build with ioc to make use of injection
        return bindings;
      });
    }

    private void InitializeMaterialDesign()
    {
      // Create dummy objects to force the MaterialDesign assemblies to be loaded
      // from this assembly
      // https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/1249
      var card = new Card();
      var hue = new Hue("Dummy", Colors.Black, Colors.White);
      var behavior = new Microsoft.Xaml.Behaviors.Media.PlaySoundAction(); //force loading of behaviors reference
    }

    private void LoadThemeResources()
    {
      Application.Current.Resources.MergedDictionaries.Add(
        Application.LoadComponent(
          new Uri("SpeckleDesktopUI;component/Themes/Generic.xaml", UriKind.Relative)
        )as ResourceDictionary);
    }

  }
}

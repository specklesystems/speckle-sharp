using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Utils;
using Stylet;
using StyletIoC;

namespace Speckle.DesktopUI
{
  public class Bootstrapper : Bootstrapper<RootViewModel>
  {
    // TODO register connector bindings
    public ConnectorBindings Bindings;

    protected override void OnStart()
    {
      base.OnStart();

      InitializeMaterialDesign();
      LoadThemeResources();
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

      builder.Bind<ConnectorBindings>().ToInstance(Bindings);
    }

    protected override void OnLaunch()
    {
      base.OnLaunch();
    }

    private void InitializeMaterialDesign()
    {
      // Create dummy objects to force the MaterialDesign assemblies to be loaded
      // from this assembly
      // https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/1249
      var card = new Card();
      var hue = new Hue("Dummy", Colors.Black, Colors.White);
    }

    private void LoadThemeResources()
    {
      Application.Current.Resources.MergedDictionaries.Add(
        Application.LoadComponent(
          new Uri("SpeckleDesktopUI;component/Themes/Generic.xaml", UriKind.Relative)
        ) as ResourceDictionary);
    }

  }
}

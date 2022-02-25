using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Logging;
using Speckle.DesktopUI.Streams;
using Speckle.DesktopUI.Utils;
using Stylet;
using Stylet.Xaml;
using StyletIoC;

namespace Speckle.DesktopUI
{
  public class Bootstrapper : Bootstrapper<RootViewModel>
  {
    public ConnectorBindings Bindings = new DummyBindings();

    private Window _rootWindow;

    public Window RootWindow
    {
      get => _rootWindow ?? (_rootWindow = (Window)RootViewModel.View);
    }

    protected override void OnStart()
    {
      base.OnStart();


      Core.Logging.Setup.Init(Bindings.GetHostAppName(),
        //quick hack, dui1 will be depracted soon so this works for now
        Bindings.GetHostAppName().ToLowerInvariant().Replace(" ", "").Replace("2019", "").Replace("2020", "").Replace("2021", "").Replace("2022", "").Replace("6", "").Replace("7", "").Replace("DynamoRevit", "dynamo").Replace("v18", "").Replace("v19", "").Replace("civil", "civil3d"));
      InitializeMaterialDesign();
      Stylet.Logging.LogManager.Enabled = true;
    }

    public void ShowRootView()
    {
      RootWindow.Show();
      RootWindow.Activate();
    }

    public void CloseRootView()
    {
      RootViewModel.RequestClose();
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

    public override void Start(string[] args)
    {
      // stop. get help.
      // this is getting triggered from _somewhere_ so I'm overriding it to prevent it from messing things up
    }

    public void Start(Application app)
    {
      OnStart();
      ConfigureBootstrapper();

      try
      {
        app.Resources.Add(View.ViewManagerResourceKey, GetInstance(typeof(IViewManager)));
      }
      catch (Exception e)
      {
        // already been added somewhere...
      }

      Configure();
      Launch();
      OnLaunch();
    }

    public void SetParent(IntPtr parent)
    {
      var helper = new System.Windows.Interop.WindowInteropHelper(RootWindow);
      helper.Owner = parent;
    }
  }

  /// <summary>
  /// Taken from stylet and modified to not use the application.
  /// Added to your App.xaml, this is responsible for loading the Boostrapper you specify, and Stylet's other resources
  /// </summary>
  public class StyletAppLoader : ResourceDictionary
  {
    private readonly ResourceDictionary styletResourceDictionary;

    /// <summary>
    /// Initialises a new instance of the <see cref="ApplicationLoader"/> class
    /// </summary>
    public StyletAppLoader()
    {
      styletResourceDictionary = new ResourceDictionary()
      {
        Source = new Uri("pack://application:,,,/Stylet;component/Xaml/StyletResourceDictionary.xaml",
          UriKind.Absolute)
      };
      LoadStyletResources = true;
    }

    private Bootstrapper _bootstrapper;

    /// <summary>
    /// Gets or sets the bootstrapper instance to use to start your application. This must be set.
    /// </summary>
    public Bootstrapper Bootstrapper
    {
      get => _bootstrapper;
      set
      {
        _bootstrapper = value;
      }
    }

    private bool _loadStyletResources;

    /// <summary>
    /// Gets or sets a value indicating whether to load Stylet's own resources (e.g. StyletConductorTabControl). Defaults to true.
    /// </summary>
    public bool LoadStyletResources
    {
      get => _loadStyletResources;
      set
      {
        _loadStyletResources = value;
        if (_loadStyletResources)
          MergedDictionaries.Add(styletResourceDictionary);
        else
          MergedDictionaries.Remove(styletResourceDictionary);
      }
    }
  }
}

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Autodesk.Navisworks.Api.Plugins;
using Avalonia;
using Avalonia.ReactiveUI;
using DesktopUI2;
using DesktopUI2.ViewModels;
using Speckle.ConnectorNavisworks.Bindings;
using Speckle.Core.Logging;
using Application = Autodesk.Navisworks.Api.Application;

namespace Speckle.ConnectorNavisworks.Entry
{
  [DockPanePlugin(
    400,
    400,
    FixedSize = false,
    AutoScroll = true,
    MinimumHeight = 410,
    MinimumWidth = 250)
  ]
  [Plugin(
    LaunchSpeckleConnector.Plugin,
    "Speckle",
    DisplayName = "Speckle",
    Options = PluginOptions.None,
    ToolTip = "Speckle Connector for Navisworks",
    ExtendedToolTip = "Speckle Connector for Navisworks")
  ]
  internal class SpeckleNavisworksCommandPlugin : DockPanePlugin
  {
    public override Control CreateControlPane()
    {
      AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
      Setup.Init(ConnectorBindingsNavisworks.HostAppNameVersion, ConnectorBindingsNavisworks.HostAppName);
      try
      {
        InitAvalonia();
      }
      catch
      {
        // ignore
      }

      var navisworksActiveDocument = Application.ActiveDocument;

      var bindings = new ConnectorBindingsNavisworks(navisworksActiveDocument);
      bindings.RegisterAppEvents();
      var viewModel = new MainViewModel(bindings);

      Analytics.TrackEvent(Analytics.Events.Registered, null, false);

      var speckleHost = new ElementHost
      {
        AutoSize = true,
        Child = new SpeckleHostPane
        {
          DataContext = viewModel
        }
      };

      speckleHost.CreateControl();

      return speckleHost;
    }

    public override void DestroyControlPane(Control pane)
    {
      if (pane is UserControl control) control.Dispose();
    }

    public static AppBuilder BuildAvaloniaApp()
    {
      var app = AppBuilder.Configure<App>();

      app.UsePlatformDetect();
      app.With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 });
      app.With(new Win32PlatformOptions
        { AllowEglInitialization = true, EnableMultitouch = false, UseWgl = false });
      app.LogToTrace();
      app.UseReactiveUI();

      return app;
    }

    public static void InitAvalonia()
    {
      BuildAvaloniaApp().SetupWithoutStarting();
    }

    private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly a = null;
      var name = args.Name.Split(',')[0];
      var path = Path.GetDirectoryName(typeof(RibbonHandler).Assembly.Location);

      var assemblyFile = Path.Combine(path ?? string.Empty, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);

      return a;
    }
  }
}
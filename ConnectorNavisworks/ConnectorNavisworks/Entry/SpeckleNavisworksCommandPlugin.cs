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
using JetBrains.Annotations;
using Speckle.ConnectorNavisworks.Bindings;
using Speckle.Core.Logging;
using Application = Autodesk.Navisworks.Api.Application;

namespace Speckle.ConnectorNavisworks.Entry;

[
  DockPanePlugin(450, 750, FixedSize = false, AutoScroll = true, MinimumHeight = 410, MinimumWidth = 250),
  Plugin(
    LaunchSpeckleConnector.Plugin,
    "Speckle",
    DisplayName = "Speckle",
    Options = PluginOptions.None,
    ToolTip = "Speckle Connector for Navisworks",
    ExtendedToolTip = "Speckle Connector for Navisworks"
  ), UsedImplicitly]
internal sealed class SpeckleNavisworksCommandPlugin : DockPanePlugin
{
  internal ConnectorBindingsNavisworks Bindings;

  public override Control CreateControlPane()
  {
    AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

    Setup.Init(ConnectorBindingsNavisworks.HostAppNameVersion, ConnectorBindingsNavisworks.HostAppName);
    
    InitAvalonia();

    var navisworksActiveDocument = Application.ActiveDocument;

    Bindings = new ConnectorBindingsNavisworks(navisworksActiveDocument);
    Bindings.RegisterAppEvents();
    var viewModel = new MainViewModel(Bindings);

    Analytics.TrackEvent(Analytics.Events.Registered, null, false);

    var speckleHost = new ElementHost
    {
      AutoSize = true,
      Child = new SpeckleHostPane { DataContext = viewModel }
    };

    speckleHost.CreateControl();

    return speckleHost;
  }

  private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
  {
    var ex = (Exception)e.ExceptionObject;

    SpeckleLog.Logger.Fatal(ex, "Navisworks Error: {error}", ex.Message);
  }

  public override void DestroyControlPane(Control pane)
  {
    if (pane is UserControl control)
      control.Dispose();
  }

  private static AppBuilder BuildAvaloniaApp()
  {
    var app = AppBuilder.Configure<App>();

    app.UsePlatformDetect();
    app.With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 });
    app.With(
      new Win32PlatformOptions
      {
        AllowEglInitialization = true,
        EnableMultitouch = false,
        UseWgl = false
      }
    );
    app.LogToTrace();
    app.UseReactiveUI();

    return app;
  }

  private static void InitAvalonia()
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

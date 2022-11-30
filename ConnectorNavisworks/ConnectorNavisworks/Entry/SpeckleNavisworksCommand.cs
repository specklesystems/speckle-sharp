using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;
using Autodesk.Navisworks.Api.Plugins;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using Speckle.ConnectorNavisworks.Bindings;
using Speckle.Core.Logging;
using Control = System.Windows.Forms.Control;
using UserControl = System.Windows.Forms.UserControl;

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
            try
            {
                InitAvalonia();
            }
            catch
            {
                // ignore
            }

            var navisworksActiveDocument = Autodesk.Navisworks.Api.Application.ActiveDocument;

            var bindings = new ConnectorBindingsNavisworks(navisworksActiveDocument);
            bindings.RegisterAppEvents();
            var viewModel = new MainViewModel(bindings);

            Setup.Init(bindings.GetHostAppNameVersion(), bindings.GetHostAppName());
            Analytics.TrackEvent(Analytics.Events.Registered, null, false);

            ElementHost speckleHost = new ElementHost()
            {
                AutoSize = true,
                Child = new SpeckleHostPane()
                {
                    DataContext = viewModel
                }
            };

            speckleHost.CreateControl();

            return speckleHost;
        }

        public override void DestroyControlPane(Control pane)
        {
            if (pane is UserControl control)
            {
                control.Dispose();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            AppBuilder app = AppBuilder.Configure<DesktopUI2.App>();

            app.UsePlatformDetect();
            app.With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 });
            app.With(new Win32PlatformOptions
                { AllowEglInitialization = true, EnableMultitouch = false, UseWgl = true });
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
            string path = Path.GetDirectoryName(typeof(RibbonHandler).Assembly.Location);

            string assemblyFile = Path.Combine(path, name + ".dll");

            if (File.Exists(assemblyFile))
                a = Assembly.LoadFrom(assemblyFile);

            return a;
        }
    }
}
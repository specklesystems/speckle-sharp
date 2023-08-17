using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using System.Timers;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CSiAPIv1;
using Speckle.ConnectorCSI.UI;

using System.Reflection;
using System.IO;

namespace SpeckleConnectorCSI
{
  public class cPlugin
  {
    public static cPluginCallback pluginCallback { get; set; }
    public static bool isSpeckleClosed { get; set; } = false;
    public Timer SelectionTimer;
    public static cSapModel model { get; set; }

    public static Window MainWindow { get; private set; }

    public static ConnectorBindingsCSI Bindings { get; set; }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
      .UsePlatformDetect()
      .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
      .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
      .LogToTrace()
      .UseReactiveUI();


    public static void CreateOrFocusSpeckle()
    {
      if (MainWindow == null)
      {
        BuildAvaloniaApp().Start(AppMain, null);
      }

      MainWindow.Show();
      MainWindow.Activate();
    }

    private static void AppMain(Application app, string[] args)
    {
      var viewModel = new MainViewModel(Bindings);

      var streams = Bindings.GetStreamsInFile();
      streams = streams ?? new List<DesktopUI2.Models.StreamState>();
      Bindings.UpdateSavedStreams?.Invoke(streams);

      MainWindow = new MainWindow { DataContext = viewModel };
      MainWindow.Closed += SpeckleWindowClosed;
      MainWindow.Closing += SpeckleWindowClosed;
      app.Run(MainWindow);
      //Task.Run(() => app.Run(MainWindow));
    }

    public static void OpenOrFocusSpeckle(cSapModel model)
    {
      Bindings = new ConnectorBindingsCSI(model);
      CreateOrFocusSpeckle();
    }

    private static void SpeckleWindowClosed(object sender, EventArgs e)
    {
      isSpeckleClosed = true;
      Process[] processCollection = Process.GetProcesses();
      foreach (Process p in processCollection)
      {
        if (p.ProcessName == "DriverCSharp")
          Environment.Exit(0);
      }
      //Environment.Exit(0);
      pluginCallback.Finish(0);
    }

    public int Info(ref string Text)
    {
      Text = "This is a Speckle plugin for CSI Products";
      return 0;
    }

    public static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly a = null;
      var name = args.Name.Split(',')[0];
      string path = Path.GetDirectoryName(typeof(cPlugin).Assembly.Location);

      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);

      return a;
    }

    public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
    {
      cSapModel model;
      pluginCallback = ISapPlugin;
      AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);
      model = SapModel;
      AppDomain domain = null;

      try
      {
        OpenOrFocusSpeckle(model);
      }
      catch (Exception e)
      {
        throw;
        ISapPlugin.Finish(0);
        //return;
      }

      return;
    }
  }


}

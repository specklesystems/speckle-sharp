using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.ConnectorTeklaStructures.UI;
using Speckle.Core.Logging;
using Tekla.Structures.Dialog;
using Tekla.Structures.Model;
using Assembly = System.Reflection.Assembly;

namespace Speckle.ConnectorTeklaStructures
{
  public partial class MainForm : PluginFormBase
  {

    //window owner call
    [DllImport("user32.dll", SetLastError = true)]
    [SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes")]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);
    const int GWL_HWNDPARENT = -8;

    private static Avalonia.Application AvaloniaApp { get; set; }
    public Model Model { get; private set; }
    public static Window MainWindow { get; private set; }
    public static ConnectorBindingsTeklaStructures Bindings { get; set; }
    public MainForm()
    {
      Load += MainForm_Load;
      if (MainWindow == null)
      {
        // Link to model.         
        Model = new Model();
        Bindings = new ConnectorBindingsTeklaStructures(Model);

        try
        {
          AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);


          Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());
          BuildAvaloniaApp().Start(AppMain, null);
          var viewModel = new MainViewModel(Bindings);
          MainWindow = new DesktopUI2.Views.MainWindow
          {
            DataContext = viewModel
          };



          Bindings.OpenTeklaStructures();

        }
        catch (Exception ex)
        {
          SpeckleLog.Logger.Fatal(ex, "Failed to create main form");
        }
      }

      MainWindow.Show();
      MainWindow.Activate();
      MainWindow.Focus();

      //set Tekla app as owner
      var hwnd = MainWindow.PlatformImpl.Handle.Handle;
      SetWindowLongPtr(hwnd, GWL_HWNDPARENT, Tekla.Structures.Dialog.MainWindow.Frame.Handle);
    }


    static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly a = null;
      var name = args.Name.Split(',')[0];
      string path = Path.GetDirectoryName(typeof(MainPlugin).Assembly.Location);

      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);

      return a;
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
        .UsePlatformDetect()
        .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
        .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
        .LogToTrace()
        .UseReactiveUI();

    private static void AppMain(Application app, string[] args)
    {
      AvaloniaApp = app;
    }


    private void MainForm_Load(object sender, EventArgs e)
    {
      Close();
    }

  }
}

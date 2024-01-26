using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using Speckle.ConnectorTeklaStructures.UI;
using Speckle.Core.Logging;
using Tekla.Structures.Dialog;
using Tekla.Structures.Model;
using Assembly = System.Reflection.Assembly;

namespace Speckle.ConnectorTeklaStructures;

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

  /// <summary>
  /// Initializes a new instance of the MainForm class.
  /// </summary>
  public MainForm()
  {
    InitializeComponents();
    SetupApplication();
  }

  /// <summary>
  /// Initializes the components of the MainForm.
  /// </summary>
  private void InitializeComponents()
  {
    Load += MainForm_Load;

    if (MainWindow != null)
    {
      return;
    }

    Model = new Model();
    Bindings = new ConnectorBindingsTeklaStructures(Model);
  }

  /// <summary>
  /// Sets up the application, including event subscriptions, UI build, and model bindings.
  /// </summary>
  [SuppressMessage(
    category: "Design",
    checkId: "CA1031:Do not catch general exception types",
    Justification = "Exception is logged and plugin window doesn't populate."
  )]
  private static void SetupApplication()
  {
    if (MainWindow == null)
    {
      try
      {
        SubscribeToEvents();
        BuildAndShowMainWindow();
        SetTeklaAsOwner();
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Failed to create main form interface with {errorMessage}", ex.Message);
        MainWindow = null;
        return;
      }
    }

    MainWindow?.Show();
    MainWindow?.Activate();
    MainWindow?.Focus();
  }

  /// <summary>
  /// Subscribes to necessary application events.
  /// </summary>
  private static void SubscribeToEvents() =>
    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

  /// <summary>
  /// Builds and displays the main window of the application.
  /// </summary>
  private static void BuildAndShowMainWindow()
  {
    Setup.Init(Bindings.GetHostAppNameVersion(), Bindings.GetHostAppName());
    BuildAvaloniaApp().Start(AppMain, null);

    var viewModel = new MainViewModel(Bindings);
    MainWindow = new DesktopUI2.Views.MainWindow { DataContext = viewModel };

    Bindings.OpenTeklaStructures();
  }

  /// <summary>
  /// Sets the Tekla application as the owner of the main window.
  /// </summary>
  private static void SetTeklaAsOwner()
  {
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
    {
      a = Assembly.LoadFrom(assemblyFile);
    }

    return a;
  }

  public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder
      .Configure<DesktopUI2.App>()
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

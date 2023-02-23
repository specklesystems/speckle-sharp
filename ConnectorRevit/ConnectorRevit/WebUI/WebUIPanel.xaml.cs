using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using CefSharp;
using CefSharp.Wpf;
using Sentry.Protocol;
using WebUI;

namespace Speckle.ConnectorRevit
{
  public partial class WebUIPanel : Page, Autodesk.Revit.UI.IDockablePaneProvider
  {
    public WebUIPanel(WebUIBindings webUIBindings, string address = "https://appui.speckle.systems")
    {
      //InitializeCef();
      InitializeComponent();
      webUIBindings.Browser = Browser;

      //CefSharpSettings.LegacyJavascriptBindingEnabled = true;

#if (REVIT2022)
      // old method
      Browser.RegisterAsyncJsObject("UiBindings", webUIBindings);
#else
      // new method
      Browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
      Browser.JavascriptObjectRepository.Register("UiBindings", webUIBindings, isAsync: true, options: BindingOptions.DefaultBinder);
#endif
      Browser.Address = address;
    }

    // Note: Dynamo ships with cefsharp too, so we need to be careful around initialising cefsharp.
    private void InitializeCef()
    {
      if (Cef.IsInitialized) return;

      Cef.EnableHighDPISupport();

      var assemblyLocation = Assembly.GetExecutingAssembly().Location;
      var assemblyPath = System.IO.Path.GetDirectoryName(assemblyLocation);
      var pathSubprocess = System.IO.Path.Combine(assemblyPath, "CefSharp.BrowserSubprocess.exe");
      var settings = new CefSettings
      {
        BrowserSubprocessPath = pathSubprocess,
        RemoteDebuggingPort = 9222
      };

      Cef.Initialize(settings);
    }

    public void SetupDockablePane(Autodesk.Revit.UI.DockablePaneProviderData data)
    {
      data.FrameworkElement = this as FrameworkElement;
      data.InitialState = new Autodesk.Revit.UI.DockablePaneState();
      data.InitialState.DockPosition = DockPosition.Tabbed;
      data.InitialState.TabBehind = Autodesk.Revit.UI.DockablePanes.BuiltInDockablePanes.ProjectBrowser;
    }
  }
}

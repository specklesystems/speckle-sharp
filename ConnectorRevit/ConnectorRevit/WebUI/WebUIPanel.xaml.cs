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

#if DEBUG
    public WebUIPanel(WebUIBindings webUIBindings, string address = "http://localhost:8080")
#else
 public WebUIPanel(WebUIBindings webUIBindings, string address = "https://dashing-haupia-e8f6e3.netlify.app/")
#endif
    {
      InitializeComponent();
      Browser.FrameLoadEnd += Browser_FrameLoadEnd; ;
      webUIBindings.Browser = Browser;



#if (REVIT2022)
      // old method
      //CefSharpSettings.LegacyJavascriptBindingEnabled = true;
      Browser.RegisterAsyncJsObject("UiBindings", webUIBindings, options: BindingOptions.DefaultBinder);


#else
      // new method
      Browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
      Browser.JavascriptObjectRepository.Register("UiBindings", webUIBindings, isAsync: true, options: BindingOptions.DefaultBinder);
#endif
      Browser.Address = address;

    }

    private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
    {
      Browser.ShowDevTools();
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

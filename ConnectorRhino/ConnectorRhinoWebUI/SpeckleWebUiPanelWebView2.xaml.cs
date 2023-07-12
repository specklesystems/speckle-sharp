using System;
using System.Windows.Controls;
using DUI3;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Interaction logic for SpeckleWebUiPanelWebView2.xaml
  /// </summary>
  public partial class SpeckleWebUiPanelWebView2 : UserControl
  {
    public SpeckleWebUiPanelWebView2()
    {
      InitializeComponent();
      Browser.CoreWebView2InitializationCompleted += Browser_Initialized_Completed;
    }

    private void Browser_Initialized_Completed(object sender, EventArgs e)
    {
      Browser.CoreWebView2.OpenDevToolsWindow();
      var executeScriptAsyncMethod = (string script) => { Browser.ExecuteScriptAsync(script); };

      var baseBindings = new RhinoBaseBindings(); // They don't need to be created here, but wherever it makes sense in the app
      var baseBindingsBridge = new DUI3.BrowserBridge(Browser, baseBindings, executeScriptAsyncMethod);

      Browser.CoreWebView2.AddHostObjectToScript("WebUIBinding", baseBindingsBridge);
    }
  }
}

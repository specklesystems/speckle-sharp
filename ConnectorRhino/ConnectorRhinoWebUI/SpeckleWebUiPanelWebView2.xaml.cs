using System;
using System.Diagnostics;
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

      var executeScriptAsyncMethod = (string script) => {
        Debug.WriteLine(script);
        Browser.ExecuteScriptAsync(script); 
      };

      var showDevToolsMethod = () => Browser.CoreWebView2.OpenDevToolsWindow();

      // Test bindings 1
      var baseBindings = new RhinoBaseBindings();
      var baseBindingsBridge = new DUI3.BrowserBridge(Browser, baseBindings, executeScriptAsyncMethod, showDevToolsMethod);

      // Test bindings 2
      var randomBinding = new RhinoRandomBinding();
      var randomBindingBridge = new DUI3.BrowserBridge(Browser, randomBinding, executeScriptAsyncMethod, showDevToolsMethod);

      Browser.CoreWebView2.AddHostObjectToScript(baseBindingsBridge.FrontendBoundName, baseBindingsBridge);
      Browser.CoreWebView2.AddHostObjectToScript(randomBindingBridge.FrontendBoundName, randomBindingBridge);
    }
  }
}

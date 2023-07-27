using System;
using System.Diagnostics;
using System.Windows.Controls;
using ConnectorRhinoWebUI.Bindings;
using DUI3;
using DUI3.Bindings;
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

      // Base bindings
      var baseBindings = new BasicConnectorBindingRhino();
      var baseBindingsBridge = new DUI3.BrowserBridge(Browser, baseBindings, executeScriptAsyncMethod, showDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(baseBindingsBridge.FrontendBoundName, baseBindingsBridge);

      // Test bindings
      var testBinding = new TestBinding();
      var testBindingBridge = new DUI3.BrowserBridge(Browser, testBinding, executeScriptAsyncMethod, showDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(testBindingBridge.FrontendBoundName, testBindingBridge);
      
      // Config bindings
      var configBindings = new ConfigBinding();
      var configBindingsBridge = new BrowserBridge(
        Browser,
        configBindings,
        executeScriptAsyncMethod,
        showDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(configBindingsBridge.FrontendBoundName, configBindingsBridge);
      
            
      // Selection bindings
      var selectionBinding = new SelectionBinding();
      var selectionBindingBridge = new BrowserBridge(
        Browser,
        selectionBinding,
        executeScriptAsyncMethod,
        showDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(selectionBindingBridge.FrontendBoundName, selectionBindingBridge);
      
    }
  }
}

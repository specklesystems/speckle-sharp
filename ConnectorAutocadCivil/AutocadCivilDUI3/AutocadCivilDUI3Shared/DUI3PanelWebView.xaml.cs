using System.Diagnostics;
using System.Windows.Controls;
using DUI3;
using DUI3.Bindings;
using Microsoft.Web.WebView2.Core;
using Speckle.ConnectorAutocadDUI3.Bindings;

namespace Speckle.ConnectorAutocadDUI3;

public partial class DUI3PanelWebView : UserControl
{
  public DUI3PanelWebView()
  {
    InitializeComponent();
    Browser.CoreWebView2InitializationCompleted += Browser_Initialized_Completed;
  }

  private void Browser_Initialized_Completed(object sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    var executeScriptAsyncMethod = (string script) => {
      Debug.WriteLine(script);
      Browser.ExecuteScriptAsync(script); 
    };

    var showDevToolsMethod = () => Browser.CoreWebView2.OpenDevToolsWindow();

    // Test bindings 1
    var baseBindings = new BasicConnectorBindingAutocad();
    var baseBindingsBridge = new DUI3.BrowserBridge(Browser, baseBindings, executeScriptAsyncMethod, showDevToolsMethod);

    // Test bindings 2
    var testBinding = new TestBinding();
    var testBindingBridge = new DUI3.BrowserBridge(Browser, testBinding, executeScriptAsyncMethod, showDevToolsMethod);

    Browser.CoreWebView2.AddHostObjectToScript(baseBindingsBridge.FrontendBoundName, baseBindingsBridge);
    Browser.CoreWebView2.AddHostObjectToScript(testBindingBridge.FrontendBoundName, testBindingBridge);
    
    // Config bindings
    var configBindings = new ConfigBinding();
    var configBindingsBridge = new BrowserBridge(
      Browser,
      configBindings,
      executeScriptAsyncMethod,
      showDevToolsMethod);
    Browser.CoreWebView2.AddHostObjectToScript(configBindingsBridge.FrontendBoundName, configBindingsBridge);

  }
}


using System.Diagnostics;
using System.Windows.Controls;
using DUI3;
using Microsoft.Web.WebView2.Core;
using Speckle.Core.Logging;

namespace ConnectorRhinoWebUI;

public partial class WebUiPanelWebView2 : UserControl
{
  public WebUiPanelWebView2()
  {
    InitializeComponent();
    Browser.CoreWebView2InitializationCompleted += OnInitialized;
  }

  private void ShowDevToolsMethod()
  {
    Browser.CoreWebView2.OpenDevToolsWindow();
  }

  private void ExecuteScriptAsyncMethod(string script)
  {
    if (!Browser.IsInitialized)
    {
      throw new SpeckleException("Failed to execute script, Webview2 is not initialized yet.");
    }
    Browser.ExecuteScriptAsync(script);
  }
  
  private void OnInitialized(object sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    var bindings = Bindings.Factory.CreateBindings();
    
    foreach(var binding in bindings)
    {
      var bridge = new BrowserBridge(Browser, binding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(binding.Name, bridge);
    }
  }
}


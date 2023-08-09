using System.Windows.Controls;
using AutocadCivilDUI3Shared.Bindings;
using DUI3;
using Microsoft.Web.WebView2.Core;
using Speckle.Core.Logging;

namespace Speckle.ConnectorAutocadDUI3;

public partial class DUI3PanelWebView : UserControl
{
  public DUI3PanelWebView()
  {
    InitializeComponent();
    Browser.CoreWebView2InitializationCompleted += Browser_Initialized_Completed;
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

  private void Browser_Initialized_Completed(object sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    var bindings = Factory.CreateBindings();

    foreach (var binding in bindings)
    {
      var bridge = new BrowserBridge(Browser, binding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(binding.Name, bridge);
    }
  }
}


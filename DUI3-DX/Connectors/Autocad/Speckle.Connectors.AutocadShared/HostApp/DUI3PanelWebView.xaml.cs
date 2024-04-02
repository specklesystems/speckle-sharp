using System.Windows.Controls;
using System.Windows.Threading;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Logging;
using Microsoft.Web.WebView2.Core;

namespace Speckle.Connectors.Autocad.HostApp;

public partial class Dui3PanelWebView : UserControl
{
  private readonly IEnumerable<Lazy<IBinding>> _bindings;

  public Dui3PanelWebView(IEnumerable<Lazy<IBinding>> bindings)
  {
    _bindings = bindings;
    InitializeComponent();
    Browser.CoreWebView2InitializationCompleted += Browser_Initialized_Completed;
  }

  private void ShowDevToolsMethod() => Browser.CoreWebView2.OpenDevToolsWindow();

  private void ExecuteScriptAsyncMethod(string script)
  {
    if (!Browser.IsInitialized)
    {
      throw new SpeckleException("Failed to execute script, Webview2 is not initialized yet.");
    }
    Browser.Dispatcher.Invoke(() => Browser.ExecuteScriptAsync(script), DispatcherPriority.Background);
  }

  private void Browser_Initialized_Completed(object sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    foreach (Lazy<IBinding> lazyBinding in _bindings)
    {
      var binding = lazyBinding.Value;
      binding.Parent.AssociateWithBinding(binding, ExecuteScriptAsyncMethod, Browser);
      Browser.CoreWebView2.AddHostObjectToScript(binding.Name, binding.Parent);
    }
  }
}

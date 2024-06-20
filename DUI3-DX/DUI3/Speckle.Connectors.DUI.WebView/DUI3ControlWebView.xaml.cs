using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;

namespace Speckle.Connectors.DUI.WebView;

public sealed partial class DUI3ControlWebView : UserControl
{
  private readonly IEnumerable<Lazy<IBinding>> _bindings;
  private readonly ITopLevelExceptionHandler _topLevelExceptionHandler;

  public DUI3ControlWebView(IEnumerable<Lazy<IBinding>> bindings, ITopLevelExceptionHandler topLevelExceptionHandler)
  {
    _bindings = bindings;
    _topLevelExceptionHandler = topLevelExceptionHandler;
    InitializeComponent();

    Browser.CoreWebView2InitializationCompleted += (sender, args) =>
      _topLevelExceptionHandler.CatchUnhandled(() => OnInitialized(sender, args));
  }

  private void ShowDevToolsMethod() => Browser.CoreWebView2.OpenDevToolsWindow();

  private void ExecuteScriptAsyncMethod(string script)
  {
    if (!Browser.IsInitialized)
    {
      throw new InvalidOperationException("Failed to execute script, Webview2 is not initialized yet.");
    }

    Browser.Dispatcher.Invoke(() => Browser.ExecuteScriptAsync(script), DispatcherPriority.Background);
  }

  private void OnInitialized(object? sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    if (!e.IsSuccess)
    {
      throw new InvalidOperationException("Webview Failed to initialize", e.InitializationException);
    }

    // We use Lazy here to delay creating the binding until after the Browser is fully initialized.
    // Otherwise the Browser cannot respond to any requests to ExecuteScriptAsyncMethod
    foreach (Lazy<IBinding> lazyBinding in _bindings)
    {
      SetupBinding(lazyBinding.Value);
    }
  }

  private void SetupBinding(IBinding binding)
  {
    binding.Parent.AssociateWithBinding(binding, ExecuteScriptAsyncMethod, Browser, ShowDevToolsMethod);
    Browser.CoreWebView2.AddHostObjectToScript(binding.Name, binding.Parent);
  }
}

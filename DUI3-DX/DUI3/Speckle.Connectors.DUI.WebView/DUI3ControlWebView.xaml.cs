using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Core.Logging;

namespace Speckle.Connectors.DUI.WebView;

public sealed partial class DUI3ControlWebView : UserControl
{
  private readonly IReadOnlyCollection<IBinding> _bindings;

  public DUI3ControlWebView(IReadOnlyCollection<IBinding> bindings)
  {
    _bindings = bindings;
    InitializeComponent();
    Browser.CoreWebView2InitializationCompleted += OnInitialized;
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
    foreach (IBinding binding in _bindings)
    {
      binding.Parent.AssociateWithBinding(binding, ExecuteScriptAsyncMethod, Browser);
      Browser.CoreWebView2.AddHostObjectToScript(binding.Name, binding.Parent);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Rhino7.HostApp;

public partial class SpeckleRhinoPanel : UserControl
{
  private readonly IEnumerable<Lazy<IBinding>> _bindings;

  public SpeckleRhinoPanel(IEnumerable<Lazy<IBinding>> bindings)
  {
    _bindings = bindings;
    InitializeComponent();
    Browser.CoreWebView2InitializationCompleted += OnInitialized;
  }

  public void ShowDevToolsMethod() => Browser.CoreWebView2.OpenDevToolsWindow();

  public void ExecuteScriptAsyncMethod(string script)
  {
    if (!Browser.IsInitialized)
    {
      throw new SpeckleException("Failed to execute script, Webview2 is not initialized yet.");
    }

    Browser.Dispatcher.Invoke(() => Browser.ExecuteScriptAsync(script), DispatcherPriority.Background);
  }

  private void OnInitialized(object sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    //TODO: Pass bindings to browser bridge here!
    foreach (Lazy<IBinding> lazyBinding in _bindings)
    {
      var binding = lazyBinding.Value;
      binding.Parent.AssociateWithBinding(binding, ExecuteScriptAsyncMethod, Browser);
      Console.WriteLine();
      Browser.CoreWebView2.AddHostObjectToScript(binding.Name, binding.Parent);
    }
  }
}

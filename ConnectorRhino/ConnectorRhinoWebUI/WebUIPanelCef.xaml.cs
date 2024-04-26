using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CefSharp;
using DUI3;

namespace ConnectorRhinoWebUI;

public partial class WebUiPanelCef : UserControl
{
  public WebUiPanelCef()
  {
    CefSharpSettings.ConcurrentTaskExecution = true;
    InitializeComponent();
    Browser.IsBrowserInitializedChanged += OnInitialized;
  }

  private void ShowDevToolsMethod() => Browser.ShowDevTools();

  private void ExecuteScriptAsyncMethod(string script) =>
    Browser.Dispatcher.Invoke(() => Browser.EvaluateScriptAsync(script), DispatcherPriority.Background);

  private void OnInitialized(object sender, DependencyPropertyChangedEventArgs e)
  {
    Browser.JavascriptObjectRepository.NameConverter = null;

    List<IBinding> bindings = Bindings.Factory.CreateBindings();
    foreach (IBinding binding in bindings)
    {
      BrowserBridge bridge = new(Browser, binding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
      Browser.JavascriptObjectRepository.Register(bridge.FrontendBoundName, bridge, true);
    }
  }
}

using System.Windows;
using System.Windows.Controls;
using CefSharp;
using DUI3;
using Speckle.Core.Logging;

namespace ConnectorRhinoWebUI;

public partial class WebUiPanelCef : UserControl
{
  public WebUiPanelCef()
  {
    CefSharpSettings.ConcurrentTaskExecution = true;
    InitializeComponent();
    Browser.IsBrowserInitializedChanged += OnInitialized;
  }

  private void ShowDevToolsMethod()
  {
    Browser.ShowDevTools();
  }

  private void ExecuteScriptAsyncMethod(string script)
  {
    Browser.EvaluateScriptAsync(script);
  }
  
  private void OnInitialized(object sender, DependencyPropertyChangedEventArgs e)
  {
    Browser.JavascriptObjectRepository.NameConverter = null;
    
    var bindings = Bindings.Factory.CreateBindings();
    foreach(var binding in bindings)
    {
      var bridge = new BrowserBridge(Browser, binding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
      Browser.JavascriptObjectRepository.Register(bridge.FrontendBoundName, bridge, true);
    }
  }
}


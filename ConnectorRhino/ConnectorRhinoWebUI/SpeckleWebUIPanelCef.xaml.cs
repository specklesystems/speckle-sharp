using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using CefSharp.JavascriptBinding;
using CefSharp.Wpf;
using ConnectorRhinoWebUI.Bindings;
using DUI3;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Interaction logic for SpeckleWebUIPanelCef.xaml
  /// </summary>
  public partial class SpeckleWebUIPanelCef : UserControl
  {
    public SpeckleWebUIPanelCef()
    {
      
      CefSharpSettings.ConcurrentTaskExecution = true;

      InitializeComponent();
      Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
    }

    private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (Browser.IsBrowserInitialized)
        Browser.ShowDevTools();
      
      // All this sounds like a factory function of sorts, somewhere.

      var executeScriptAsyncMethod = (string script) => {
        Debug.WriteLine(script);
        Browser.EvaluateScriptAsync(script);
      };

      var showDevToolsMethod = () => Browser.ShowDevTools();

      var baseBindings = new BasicConnectorBindingRhino(); // They don't need to be created here, but wherever it makes sense in the app
      var baseBindingsBridge = new DUI3.BrowserBridge(Browser, baseBindings, executeScriptAsyncMethod, showDevToolsMethod);

      var testBinding = new TestBinding();
      var testBindingBridge = new DUI3.BrowserBridge(Browser, testBinding, executeScriptAsyncMethod, showDevToolsMethod);

      // NOTE: could be moved - later - in the bridge class itself. Alternatively, we might need an abstraction that does all the work here
      // ie, takes a binding and lobs it into the browser.
      Browser.JavascriptObjectRepository.NameConverter = null;
      Browser.JavascriptObjectRepository.Register(baseBindingsBridge.FrontendBoundName, baseBindingsBridge, true);
      Browser.JavascriptObjectRepository.Register(testBindingBridge.FrontendBoundName, testBindingBridge, true);
    }
  }
}

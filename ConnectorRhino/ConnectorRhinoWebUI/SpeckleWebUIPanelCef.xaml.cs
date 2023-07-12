using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using CefSharp.JavascriptBinding;
using CefSharp.Wpf;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Interaction logic for SpeckleWebUIPanelCef.xaml
  /// </summary>
  public partial class SpeckleWebUIPanelCef : UserControl
  {
    public SpeckleWebUIPanelCef()
    {
      InitializeComponent();
      Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
      Browser.JavascriptObjectRepository.ResolveObject += JavascriptObjectRepository_ResolveObject;
      //Browser.JavascriptObjectRepository.Register()
    }

    private void JavascriptObjectRepository_ResolveObject(object sender, CefSharp.Event.JavascriptBindingEventArgs e)
    {
      //var repo = e.ObjectRepository;
      //if (e.ObjectName == "WebUIBinding")
      //{
      //  try
      //  {
      //    repo.NameConverter = new CamelCaseJavascriptNameConverter();
      //    repo.Register("WebUIBinding", new RhinoCefWebUIBinding(Browser), true, BindingOptions.DefaultBinder);
      //  }
      //  catch (Exception ex)
      //  {
      //    // NOTE: On page refreshes, this gets re-executed and throws an error saying that stuff's already registered.
      //    // A TODO would be to investigate if this the safest/most correct way of dealing with this problem, or simply
      //    // having a semaphore saying we did it and just not do it again. For now, letting it throw.
      //    Debug.Write(ex);
      //  }
      //}
    }

    private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (Browser.IsBrowserInitialized)
        Browser.ShowDevTools();
      
      var executeScriptAsyncMethod = (string script) => { Browser.ExecuteScriptAsync(script); };

      var baseBindings = new RhinoBaseBindings(); // They don't need to be created here, but wherever it makes sense in the app
      var baseBindingsBridge = new DUI3.BrowserBridge(Browser, baseBindings, executeScriptAsyncMethod);

      Browser.JavascriptObjectRepository.Register(baseBindings.Name, baseBindingsBridge, true);
    }
  }
}

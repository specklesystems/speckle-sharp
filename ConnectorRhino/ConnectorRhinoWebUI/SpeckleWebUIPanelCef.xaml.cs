using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using CefSharp.JavascriptBinding;
using CefSharp.Wpf;
using ConnectorRhinoWebUI.Bindings;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;

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

    private void ShowDevToolsMethod()
    {
      Browser.ShowDevTools();
    }

    private void ExecuteScriptAsyncMethod(string script)
    {
      Debug.WriteLine(script);
      Browser.EvaluateScriptAsync(script);
    }
    
    private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      var bindings = Bindings.Factory.CreateBindings();
      
      foreach(var binding in bindings)
      {
        var bridge = new BrowserBridge(Browser, binding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
        Browser.JavascriptObjectRepository.Register(bridge.FrontendBoundName, bridge, true);
      }
    }
  }
}

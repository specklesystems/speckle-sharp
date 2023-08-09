using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using ConnectorRhinoWebUI.Bindings;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Speckle.Core.Logging;

namespace ConnectorRhinoWebUI
{
  /// <summary>
  /// Interaction logic for SpeckleWebUiPanelWebView2.xaml
  /// </summary>
  public partial class SpeckleWebUiPanelWebView2 : UserControl
  {
    public SpeckleWebUiPanelWebView2()
    {
      InitializeComponent();
      Browser.CoreWebView2InitializationCompleted += Browser_Initialized_Completed;
    }

    private void Browser_Initialized_Completed(object sender, EventArgs e)
    {
      Browser.CoreWebView2.OpenDevToolsWindow();

      void ExecuteScriptAsyncMethod(string script)
      {
        if (!Browser.IsInitialized)
        {
          throw new SpeckleException("Failed to execute script, Webview2 is not initialized yet.");
        }

        Browser.ExecuteScriptAsync(script);
      }

      void ShowDevToolsMethod() => Browser.CoreWebView2.OpenDevToolsWindow();

      var documentState = new DocumentModelStore();

      var bindingsToProvide = new List<IBinding>();
      
      // Test bindings
      bindingsToProvide.Add(new TestBinding());
      bindingsToProvide.Add(new ConfigBinding());
      
      // var testBindingBridge = new DUI3.BrowserBridge(Browser, testBinding, executeScriptAsyncMethod, showDevToolsMethod);
      // Browser.CoreWebView2.AddHostObjectToScript(testBindingBridge.FrontendBoundName, testBindingBridge);

      var docState = new DocumentModelStore();
      
      // Base bindings
      var baseBindings = new BasicConnectorBindingRhino();
      var baseBindingsBridge = new DUI3.BrowserBridge(Browser, baseBindings, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(baseBindingsBridge.FrontendBoundName, baseBindingsBridge);
      
      
      // Config bindings
      var configBindings = new ConfigBinding();
      var configBindingsBridge = new BrowserBridge(
        Browser,
        configBindings,
        ExecuteScriptAsyncMethod,
        ShowDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(configBindingsBridge.FrontendBoundName, configBindingsBridge);
      
            
      // Selection bindings
      var selectionBinding = new SelectionBinding();
      var selectionBindingBridge = new BrowserBridge(
        Browser,
        selectionBinding,
        ExecuteScriptAsyncMethod,
        ShowDevToolsMethod);
      Browser.CoreWebView2.AddHostObjectToScript(selectionBindingBridge.FrontendBoundName, selectionBindingBridge);
      
    }
  }
}

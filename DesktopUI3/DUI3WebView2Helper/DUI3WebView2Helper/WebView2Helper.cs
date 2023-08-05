using System.Collections.Generic;
using System.Windows.Controls;
using DUI3;
using Microsoft.Web.WebView2.Core;
using Speckle.Core.Logging;

namespace DUI3WebView2Helper;

public static class WebView2HelperFactory
{
  public static UserControl CreateBrowserControl(List<IBinding> bindings)
  {
    var helper = new WebView2Helper(bindings);
    return helper.CreateUserControl();
  }
}

public class WebView2Helper
{
  private List<IBinding> _bindings;
  private WebView2UserControl _userControl;

  public WebView2Helper(List<IBinding> bindings)
  {
    _bindings = bindings;
  }
  
  public UserControl CreateUserControl()
  {
    _userControl = new WebView2UserControl();
    _userControl.Browser.CoreWebView2InitializationCompleted += BrowserOnCoreWebView2InitializationCompleted;

    return _userControl;
  }

  private void ShowDevToolsMethod()
  {
    if (_userControl == null || !_userControl.Browser.IsInitialized)
    {
      throw new SpeckleException("Failed to execute, Webview2 is not initialized yet.");
    }
    _userControl.Browser.CoreWebView2.OpenDevToolsWindow();
  }

  private void ExecuteScriptAsyncMethod(string script)
  {
    if (_userControl == null || !_userControl.Browser.IsInitialized)
    {
      throw new SpeckleException("Failed to execute script, Webview2 is not initialized yet.");
    }

    _userControl.Browser.ExecuteScriptAsync(script);
  }

  private void BrowserOnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
  {
    foreach (var binding in _bindings)
    {
      var bridge = new BrowserBridge(_userControl.Browser, binding, ExecuteScriptAsyncMethod, ShowDevToolsMethod);
      _userControl.Browser.CoreWebView2.AddHostObjectToScript(binding.Name, bridge);
    }
  }
}


using System.Runtime.InteropServices;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Web.WebView2.Wpf;

namespace ConnectorRhinoWebUI
{
  // NOTE: this class is going to be abstracted out; for now it lives here for hacking convenience.
  // NOTE: It might also be better as an interface??? (re feedback "we should not need to implement everything everywhere" on bindings)
  // To think about the above stuff. 
  
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  public abstract class WebUIBinding
  {
    public string GetAccounts()
    {
      var accountString = System.Text.Json.JsonSerializer.Serialize(Speckle.Core.Credentials.AccountManager.GetAccounts());
      return accountString;
    }

    public string SayHi(string name)
    {
      return $"Hi {name}! Hope you have a great day.";
    }

    public abstract string GetSourceAppName();

    public abstract void OpenDevTools();

    public abstract void SendCommand();
  }

  public abstract class RhinoWebUiBinding: WebUIBinding
  {
    public override string GetSourceAppName()
    {
      return "Rhino";
    }
  }


  public class RhinoCefWebUIBinding : RhinoWebUiBinding
  {
    private ChromiumWebBrowser Browser { get; set; }
    public RhinoCefWebUIBinding(ChromiumWebBrowser browser)
    {
      Browser = browser;
    }

    public override void OpenDevTools()
    {
      Browser.ShowDevTools();
    }

    public override void SendCommand()
    {
      // TODO, mocks for now
    }
  }

  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  public class RhinoWebView2UIBinding: RhinoWebUiBinding
  {
    private WebView2 Browser { get; set; }

    public RhinoWebView2UIBinding(WebView2 browser)
    {
      Browser = browser;
    }

    public override void OpenDevTools()
    {
      Browser.CoreWebView2.OpenDevToolsWindow();
    }

    public override void SendCommand()
    {
      // TODO, mocks for now
    }
  }
}


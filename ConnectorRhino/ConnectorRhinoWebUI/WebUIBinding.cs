using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Web.WebView2.Wpf;

namespace ConnectorRhinoWebUI
{

  // Quick hack (if we control the class, can wv2 do it properly?)
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  [Serializable]
  public class Account
  {
    public string id { get; set; }
    public Boolean isDefault { get; set; }
    public string userId { get; set; }
    public string token { get; set; }
    public string serverUrl { get;set; }
  }

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

    public Account[] GetAccountsStraight()
    {
      return Speckle.Core.Credentials.AccountManager.GetAccounts().Select(acc => new Account
      {
        id = acc.id,
        isDefault = acc.isDefault,
        userId = acc.userInfo.id,
        token = acc.token,
        serverUrl = acc.serverInfo.url
      }).ToArray();
    }

    public Account GetAccountSingleStraight()
    {
      var acc =  Speckle.Core.Credentials.AccountManager.GetAccounts().Select(acc => new Account
      {
        id = acc.id,
        isDefault = acc.isDefault,
        userId = acc.userInfo.id,
        token = acc.token,
        serverUrl = acc.serverInfo.url
      }).ToArray()[0];
      return acc;
    }

    public Dictionary<string, object> SuperTest()
    {
      var x = new Dictionary<string, object>();
      x["test"] = 1;
      x["ref"] = false;
      x["yoloo"] = "hello";

      return x;
    }

    public string[] SimpleTest()
    {
      return new string[] { "hello", "world" };
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


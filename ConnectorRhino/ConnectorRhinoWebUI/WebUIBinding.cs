using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Web.WebView2.Wpf;

namespace ConnectorRhinoWebUI
{
  // NOTE: this class is going to be abstracted out; for now it lives here for hacking convenience.
  // NOTE: It might also be better as an interface??? (re feedback "we should not need to implement everything everywhere" on bindings)
  // NOTE: Multiple objects can be registered in a browser window. This means we can have
  // DefaultBindings, SendBindings, ReceiveBindings, MapperBindings, WhateverBindings
  // To think about the above stuff. 

  public abstract class WebUIBinding
  {
    public Speckle.Core.Credentials.Account[] GetAccounts()
    {
      return Speckle.Core.Credentials.AccountManager.GetAccounts().ToArray();
    }

    public string SayHi(string name)
    {
      return $"Hi {name}! Hope you have a great day.";
    }

    public abstract string GetSourceAppName();

    public abstract void OpenDevTools();

    public abstract void SendToBrowser();

    //public abstract void GetFileState();

    //public abstract void UpdateFileState();
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

    public override void SendToBrowser()
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

    public override void SendToBrowser()
    {
      // TODO, mocks for now
    }
  }
}


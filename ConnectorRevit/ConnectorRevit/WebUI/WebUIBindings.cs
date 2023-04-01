using System;
using System.Text;
using CefSharp;
using CefSharp.Wpf;
using DesktopUI2.ViewModels;
using Speckle.ConnectorRevit.Entry;

namespace WebUI
{
  public abstract class WebUIBindings
  {
    public IWebBrowser Browser { get; set; }

    public WebUIBindings()
    {
    }

    public abstract void ShowAccountsPopup();


    public abstract void SendStream(string args);

  }
}

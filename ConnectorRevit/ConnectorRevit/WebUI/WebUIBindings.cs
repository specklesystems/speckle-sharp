using System;
using System.Text;
using CefSharp;
using CefSharp.Wpf;

namespace WebUI
{
  public abstract class WebUIBindings
  {
    public IWebBrowser Browser { get; set; }

    public WebUIBindings()
    {
    }

    public virtual void ShowAccountsPopup()
    {
      // mimic an abract function call
      SendStream(null);
    }

    public abstract void SendStream(string args);

  }
}

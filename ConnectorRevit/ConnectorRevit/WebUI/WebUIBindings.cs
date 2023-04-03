using System;
using System.Text;
using CefSharp;
using CefSharp.Wpf;
using DesktopUI2.ViewModels;
using Speckle.ConnectorRevit.Entry;
using Speckle.Newtonsoft.Json;

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

    /// <summary>
    /// Sends an event to the UI, bound to the global EventBus.
    /// </summary>
    /// <param name="eventName">The event's name.</param>
    /// <param name="eventInfo">The event args, which will be serialised to a string.</param>
    public virtual void NotifyUi(string eventName, string eventMessage)
    {
      var script = string.Format("window.EventBus.$emit('{0}', '{1}')", eventName, eventMessage);
      Browser.GetMainFrame().EvaluateScriptAsync(script);
    }

    /// <summary>
    /// Pops open the dev tools.
    /// </summary>
    public virtual void ShowDev()
    {
      Browser.ShowDevTools();
    }

  }
}

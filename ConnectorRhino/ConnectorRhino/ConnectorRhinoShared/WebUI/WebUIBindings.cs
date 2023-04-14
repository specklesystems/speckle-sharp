using System;
using System.Runtime.InteropServices;
using System.Text;
using Sentry.Protocol;

#if RHINO7 && !MAC
namespace WebUI
{
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  public abstract class WebUIBindings
  {
    public Microsoft.Web.WebView2.Core.CoreWebView2 CoreWebView2 { get; set; }
    public WebUIBindings()
    {
    }

    public abstract void ShowAccountsPopup();


    public abstract void SendStream();

    /// <summary>
    /// Sends an event to the UI, bound to the global EventBus.
    /// </summary>
    /// <param name="eventName">The event's name.</param>
    /// <param name="eventMessage">The event args, which will be serialised to a string.</param>
    public virtual void NotifyUi(string eventName, string eventMessage)
    {
      var script = string.Format("window.EventBus.$emit('{0}', '{1}')", eventName, eventMessage);
      CoreWebView2.ExecuteScriptAsync(script);
    }


    /// <summary>
    /// Pops open the dev tools.
    /// </summary>
    public virtual void ShowDev()
    {
      CoreWebView2.OpenDevToolsWindow();
    }
  }
}
#endif

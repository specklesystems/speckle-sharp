using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WebUI
{
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  public abstract class WebUIBindings
  {
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

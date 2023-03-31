using System;
using System.Runtime.InteropServices;

namespace Speckle.ConnectorRhino.UI
{
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  public class RhinoWebUIBindings : WebUI.WebUIBindings
  {
    public RhinoWebUIBindings()
    {
    }

    // sample callback from web UI
    public override void SendStream(string args)
    {

    }
  }
}

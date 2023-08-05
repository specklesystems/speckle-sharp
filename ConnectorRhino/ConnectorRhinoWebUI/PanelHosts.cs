using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using ConnectorRhinoWebUI.Bindings;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.UI;
using Command = Rhino.Commands.Command;
using IBinding = DUI3.IBinding;

namespace ConnectorRhinoWebUI
{
  [Guid("39BC44A4-C9DC-4B0A-9A51-4C31ACBCD76A")]
  public class SpeckleWebUiWebView2PanelHost : RhinoWindows.Controls.WpfElementHost
  {
    public SpeckleWebUiWebView2PanelHost(uint docSn) 
      : base(new WebUiPanelWebView2(), null)
    {
    }
  }
  
  [Guid("55B9125D-E8CA-4F65-B016-60DA932AB694")]
  public class SpeckleWebUiCefPanelHost : RhinoWindows.Controls.WpfElementHost
  {
    public SpeckleWebUiCefPanelHost(uint docSn)
      : base(new WebUiPanelCef(), null)
    {
    }
  }
}

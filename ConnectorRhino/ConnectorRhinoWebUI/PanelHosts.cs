using System.Runtime.InteropServices;

namespace ConnectorRhinoWebUI;

[Guid("39BC44A4-C9DC-4B0A-9A51-4C31ACBCD76A")]
public class SpeckleWebUiWebView2PanelHost : RhinoWindows.Controls.WpfElementHost
{
  private readonly uint _docSn;

  public SpeckleWebUiWebView2PanelHost(uint docSn)
    : base(new WebUiPanelWebView2(), null)
  {
    _docSn = docSn;
  }
}

[Guid("55B9125D-E8CA-4F65-B016-60DA932AB694")]
public class SpeckleWebUiCefPanelHost : RhinoWindows.Controls.WpfElementHost
{
  private readonly uint _docSn;

  public SpeckleWebUiCefPanelHost(uint docSn)
    : base(new WebUiPanelCef(), null)
  {
    _docSn = docSn;
  }
}

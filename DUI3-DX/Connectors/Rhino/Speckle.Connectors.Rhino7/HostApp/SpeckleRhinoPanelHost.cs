using System.Runtime.InteropServices;
using Speckle.Connectors.Rhino7.Plugin;

namespace Speckle.Connectors.Rhino7.HostApp;

[Guid("39BC44A4-C9DC-4B0A-9A51-4C31ACBCD76A")]
public class SpeckleRhinoPanelHost : RhinoWindows.Controls.WpfElementHost
{
  private readonly uint _docSn;

  public SpeckleRhinoPanelHost(uint docSn)
    : base(SpeckleConnectorsRhino7Plugin.Instance.Container?.Resolve<SpeckleRhinoPanel>(), null)
  {
    _docSn = docSn;
  }
}

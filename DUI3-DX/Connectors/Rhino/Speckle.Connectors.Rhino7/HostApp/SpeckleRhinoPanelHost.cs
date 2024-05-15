using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Rhino.UI;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Rhino7.Plugin;

namespace Speckle.Connectors.Rhino7.HostApp;

[Guid("39BC44A4-C9DC-4B0A-9A51-4C31ACBCD76A")]
public class SpeckleRhinoPanelHost : RhinoWindows.Controls.WpfElementHost
{
  private readonly uint _docSn;
  private readonly DUI3ControlWebView? _webView;

  public SpeckleRhinoPanelHost(uint docSn)
    : base(SpeckleConnectorsRhino7Plugin.Instance.Container?.Resolve<DUI3ControlWebView>(), null)
  {
    _docSn = docSn;
    _webView = SpeckleConnectorsRhino7Plugin.Instance.Container?.Resolve<DUI3ControlWebView>();
    Panels.Closed += PanelsOnClosed;
  }

  private void PanelsOnClosed(object sender, PanelEventArgs e)
  {
    if (e.PanelId == typeof(SpeckleRhinoPanelHost).GUID)
    {
      // Disconnect UIElement from WpfElementHost. Otherwise, we can't reinit panel with same DUI3ControlWebView
      if (_webView != null)
      {
        // Since WpfHost inherited from Border, find the parent as border and set null it's Child.
        if (LogicalTreeHelper.GetParent(_webView) is Border border)
        {
          border.Child = null;
        }
      }
    }
  }
}

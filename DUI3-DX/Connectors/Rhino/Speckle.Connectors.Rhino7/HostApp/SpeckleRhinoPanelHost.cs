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
      // This check comes from behavioral difference on closing Rhino Panels.
      // IsPanelVisible returns;
      //  - True, when docked Panel closed from the list on right click on panel tab,
      // whenever it is closed with this way, Rhino.Panels tries to reinit this object and expect the different UIElement, that's why we disconnect Child.
      //  - False, when detached Panel is closed by 'X' close button.
      // whenever it is closed with this way, Rhino.Panels don't create this object, that's why we do not disconnect Child UIElement.
      if (!Panels.IsPanelVisible(typeof(SpeckleRhinoPanelHost).GUID))
      {
        return;
      }

      // Unsubscribe from the event to prevent growing registrations.
      Panels.Closed -= PanelsOnClosed;

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

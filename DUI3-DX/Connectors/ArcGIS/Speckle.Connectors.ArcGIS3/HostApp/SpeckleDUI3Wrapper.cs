using System.Windows.Controls;
using Speckle.Connectors.DUI.WebView;

namespace Speckle.Connectors.ArcGIS.HostApp;

public class SpeckleDUI3Wrapper : UserControl
{
  public SpeckleDUI3Wrapper()
  {
    Initialize();
  }

  private void Initialize()
  {
    Content = SpeckleModule.Current.Container.Resolve<Dui3ControlWebView>();
  }
}

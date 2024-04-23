using System.Windows.Controls;
using Speckle.Connectors.ArcGIS.HostApp;

namespace Speckle.Connectors.ArcGIS;

public class SpeckleDUI3Wrapper : UserControl
{
  public SpeckleDUI3Wrapper()
  {
    Initialize();
  }

  private void Initialize()
  {
    Content = SpeckleModule.Current.Container.Resolve<SpeckleDUI3>();
  }
}

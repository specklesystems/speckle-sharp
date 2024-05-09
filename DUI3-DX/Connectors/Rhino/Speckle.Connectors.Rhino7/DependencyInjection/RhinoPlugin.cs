using Rhino;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Interfaces;
using Speckle.Connectors.Rhino7.Plugin;
using Speckle.Connectors.Utils;

namespace Speckle.Connectors.Rhino7.DependencyInjection;

public class RhinoPlugin : IRhinoPlugin
{
  private readonly RhinoIdleManager _idleManager;
  private readonly DUI3ControlWebView _panel;
  private readonly RhinoSettings _settings;

  public RhinoPlugin(DUI3ControlWebView panel, RhinoSettings settings, RhinoIdleManager idleManager)
  {
    _panel = panel;
    _settings = settings;
    _idleManager = idleManager;
  }

  public void Initialise()
  {
    _idleManager.SubscribeToIdle(
      () => RhinoApp.RunScript(SpeckleConnectorsRhino7Command.Instance.NotNull().EnglishName, false)
    );
  }

  public void Shutdown() { }
}

using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.Interfaces;
using Speckle.Connectors.DUI.WebView;

namespace Speckle.Connectors.Autocad.Plugin;

public class AutocadPlugin : IAutocadPlugin
{
  private readonly AutocadIdleManager _idleManager;
  private readonly DUI3ControlWebView _panel;
  private readonly AutocadSettings _settings;

  public AutocadPlugin(DUI3ControlWebView panel, AutocadSettings settings, AutocadIdleManager idleManager)
  {
    _panel = panel;
    _settings = settings;
    _idleManager = idleManager;
  }

  public void Initialise() { }

  public void Shutdown() { }
}

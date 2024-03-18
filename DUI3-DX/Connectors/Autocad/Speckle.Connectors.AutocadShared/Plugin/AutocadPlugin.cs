using Speckle.Connectors.Autocad.HostApp;

namespace Speckle.Connectors.Autocad.Plugin;

public class AutocadPlugin : IAutocadPlugin
{
  private readonly AutocadIdleManager _idleManager;
  private readonly Dui3PanelWebView _panel;
  private readonly AutocadSettings _settings;

  public AutocadPlugin(Dui3PanelWebView panel, AutocadSettings settings, AutocadIdleManager idleManager)
  {
    _panel = panel;
    _settings = settings;
    _idleManager = idleManager;
  }

  public void Initialise()
  {
    // TBD
  }

  public void Shutdown() { }
}

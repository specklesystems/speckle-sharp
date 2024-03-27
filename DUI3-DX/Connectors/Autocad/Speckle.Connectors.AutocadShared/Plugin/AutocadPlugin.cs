using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.Interfaces;

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

  public void Initialise() { }

  public void Shutdown() { }
}

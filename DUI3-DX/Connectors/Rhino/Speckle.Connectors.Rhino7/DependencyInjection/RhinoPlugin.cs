using System;
using System.Collections.Generic;
using Rhino;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Interfaces;
using Speckle.Connectors.Rhino7.Plugin;

namespace Speckle.Connectors.Rhino7.DependencyInjection;

public class RhinoPlugin : IRhinoPlugin
{
  private readonly RhinoIdleManager _idleManager;
  private readonly SpeckleRhinoPanel _panel;
  private readonly RhinoSettings _settings;

  public RhinoPlugin(SpeckleRhinoPanel panel, RhinoSettings settings, RhinoIdleManager idleManager)
  {
    _panel = panel;
    _settings = settings;
    _idleManager = idleManager;
  }

  public void Initialise()
  {
    _idleManager.SubscribeToIdle(() => RhinoApp.RunScript(SpeckleConnectorsRhino7Command.Instance.EnglishName, false));
  }

  public void Shutdown() { }
}

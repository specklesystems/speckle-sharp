using Rhino;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Plugin;
using Speckle.InterfaceGenerator;

namespace Speckle.Connectors.Rhino7.DependencyInjection;

[GenerateAutoInterface]
public class RhinoPlugin : IRhinoPlugin
{
  private readonly IRhinoIdleManager _idleManager;

  public RhinoPlugin(IRhinoIdleManager idleManager)
  {
    _idleManager = idleManager;
  }

  public void Initialise()
  {
    _idleManager.SubscribeToIdle(() => RhinoApp.RunScript(SpeckleConnectorsRhino7Command.Instance.EnglishName, false));
  }

  public void Shutdown() { }
}

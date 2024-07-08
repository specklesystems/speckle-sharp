#if CIVIL3D

using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;

namespace Speckle.Connectors.Autocad.DependencyInjection;

public class Civil3dConnectorModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    SharedRegistration.Load(builder);
    SharedRegistration.LoadSend(builder);
    
    // Register bindings
    builder.AddSingleton<IBinding, ConfigBinding>("connectorName", "Civil3d"); // POC: Easier like this for now, should be cleaned up later
  }
}
#endif

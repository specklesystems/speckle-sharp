#if AUTOCAD
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;

namespace Speckle.Connectors.Autocad.DependencyInjection;

public class AutocadConnectorModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    SharedRegistration.Load(builder);

    // Operations
    SharedRegistration.LoadSend(builder);
    SharedRegistration.LoadReceive(builder);

    // Register bindings
    builder.AddSingleton<IBinding, ConfigBinding>("connectorName", "Autocad"); // POC: Easier like this for now, should be cleaned up later
  }
}
#endif

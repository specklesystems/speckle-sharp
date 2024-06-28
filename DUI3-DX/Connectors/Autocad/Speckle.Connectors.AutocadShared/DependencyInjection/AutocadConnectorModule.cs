#if AUTOCAD
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.Bindings;
using Speckle.Connectors.Autocad.Filters;
using Speckle.Connectors.Autocad.Operations.Receive;
using Speckle.Connectors.Autocad.Operations.Send;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Autocad.DependencyInjection;

public class AutocadConnectorModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    SharedRegistration.Load(builder);

    // Operations
    builder.AddScoped<SendOperation<AutocadRootObject>>();
    builder.AddSingleton(DefaultTraversal.CreateTraversalFunc());

    // Object Builders
    builder.AddScoped<IHostObjectBuilder, AutocadHostObjectBuilder>();
    builder.AddScoped<IRootObjectBuilder<AutocadRootObject>, AutocadRootObjectBuilder>();

    // Register bindings
    builder.AddSingleton<IBinding, ConfigBinding>("connectorName", "Autocad"); // POC: Easier like this for now, should be cleaned up later
    builder.AddSingleton<IBinding, AutocadSendBinding>();
    builder.AddSingleton<IBinding, AutocadReceiveBinding>();

    // register send filters
    builder.AddTransient<ISendFilter, AutocadSelectionFilter>();

    // register send conversion cache
    builder.AddSingleton<ISendConversionCache, SendConversionCache>();
  }
}
#endif

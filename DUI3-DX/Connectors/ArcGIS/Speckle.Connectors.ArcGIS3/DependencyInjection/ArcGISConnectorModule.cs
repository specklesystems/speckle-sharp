using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.ArcGIS.Bindings;
using Speckle.Connectors.ArcGis.Operations.Send;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Converters.Common;
using Speckle.Connectors.ArcGIS.Utils;
using Speckle.Converters.ArcGIS3;
using Speckle.Connectors.ArcGIS.Operations.Receive;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Utils.Builders;
using ArcGIS.Core.Geometry;
using Speckle.Autofac;
using Speckle.Connectors.ArcGIS.Filters;
using Speckle.Connectors.DUI;
using Speckle.Connectors.Utils;
using Speckle.Converters.Common.DependencyInjection;

// POC: This is a temp reference to root object senders to tweak CI failing after having generic interfaces into common project.
// This should go whenever it is aligned.

namespace Speckle.Connectors.ArcGIS.DependencyInjection;

public class ArcGISConnectorModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddAutofac();
    builder.AddConverterCommon();
    builder.AddConnectorUtils();
    builder.AddDUI();
    builder.AddDUIView();

    builder.AddSingleton<ArcGISDocumentStore>();

    // Register bindings
    builder.AddSingleton<IBinding, TestBinding>();
    builder.AddSingleton<IBinding, ConfigBinding>("connectorName", "ArcGIS"); // POC: Easier like this for now, should be cleaned up later
    builder.AddSingleton<IBinding, AccountBinding>();
    builder.AddSingleton<IBinding, BasicConnectorBinding>();
    builder.AddSingleton<IBasicConnectorBinding, BasicConnectorBinding>();
    builder.AddSingleton<IBinding, ArcGISSelectionBinding>();
    builder.AddSingleton<IBinding, ArcGISSendBinding>();
    builder.AddSingleton<IBinding, ArcGISReceiveBinding>();

    builder.AddScoped<IHostToSpeckleUnitConverter<Unit>, ArcGISToSpeckleUnitConverter>();

    builder.AddTransient<ISendFilter, ArcGISSelectionFilter>();
    builder.AddScoped<IHostObjectBuilder, HostObjectBuilder>();

    // register send operation and dependencies
    builder.AddScoped<SendOperation>();
    builder.AddScoped<RootObjectBuilder>();
  }
}

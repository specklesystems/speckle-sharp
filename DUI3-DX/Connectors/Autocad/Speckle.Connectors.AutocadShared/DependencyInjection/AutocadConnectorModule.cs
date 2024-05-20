using Speckle.Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.Bindings;
using Speckle.Connectors.Autocad.Filters;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.Interfaces;
using Speckle.Connectors.Autocad.Operations.Receive;
using Speckle.Connectors.Autocad.Operations.Send;
using Speckle.Connectors.Autocad.Plugin;
using Speckle.Connectors.DUI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Autocad.DependencyInjection;

public class AutocadConnectorModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddAutofac();
    builder.AddConnectorUtils();
    builder.AddDUI();
    builder.AddDUIView();

    // Register other connector specific types
    builder.AddSingleton<IAutocadPlugin, AutocadPlugin>();
    builder.AddTransient<TransactionContext>();
    builder.AddSingleton(new AutocadDocumentManager()); // TODO: Dependent to TransactionContext, can be moved to AutocadContext
    builder.AddSingleton<DocumentModelStore, AutocadDocumentStore>();
    builder.AddSingleton<AutocadContext>();
    builder.AddSingleton<AutocadLayerManager>();
    builder.AddSingleton<AutocadIdleManager>();

    // Operations
    builder.AddScoped<SendOperation<AutocadRootObject>>();
    builder.AddSingleton(DefaultTraversal.CreateTraversalFunc());

    // Object Builders
    builder.AddScoped<IHostObjectBuilder, AutocadHostObjectBuilder>();
    builder.AddSingleton<IRootObjectBuilder<AutocadRootObject>, AutocadRootObjectBuilder>();

    // Register bindings

    builder.AddSingleton<IBinding, TestBinding>();
    builder.AddSingleton<IBinding, ConfigBinding>("connectorName", "Autocad"); // POC: Easier like this for now, should be cleaned up later
    builder.AddSingleton<IBinding, AccountBinding>();
    builder.AddSingleton<IBinding, AutocadBasicConnectorBinding>();
    builder.AddSingleton<IBasicConnectorBinding, AutocadBasicConnectorBinding>();
    builder.AddSingleton<IBinding, AutocadSelectionBinding>();
    builder.AddSingleton<IBinding, AutocadSendBinding>();
    builder.AddSingleton<IBinding, AutocadReceiveBinding>();

    // register send filters
    builder.AddTransient<ISendFilter, AutocadSelectionFilter>();
  }
}

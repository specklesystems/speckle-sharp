using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.Bindings;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.Interfaces;
using Speckle.Connectors.Autocad.Plugin;
using Speckle.Connectors.DUI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Autocad.Filters;
using Speckle.Connectors.Autocad.Operations.Send;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Operations;
using Speckle.Connectors.Utils.Instances;
using Speckle.Connectors.Autocad.Operations.Receive;

using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Autocad.DependencyInjection;

public static class SharedRegistration
{
  public static void Load(SpeckleContainerBuilder builder)
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
    builder.AddScoped<AutocadLayerManager>();
    builder.AddSingleton<AutocadIdleManager>();

    // Register bindings
    builder.AddSingleton<IBinding, TestBinding>();
    builder.AddSingleton<IBinding, AccountBinding>();
    builder.AddSingleton<IBinding, AutocadBasicConnectorBinding>();
    builder.AddSingleton<IBasicConnectorBinding, AutocadBasicConnectorBinding>();
    builder.AddSingleton<IBinding, AutocadSelectionBinding>();
  }

  public static void LoadSend(SpeckleContainerBuilder builder)
  {
    // Operations
    builder.AddScoped<SendOperation<AutocadRootObject>>();

    // Object Builders
    builder.AddScoped<IRootObjectBuilder<AutocadRootObject>, AutocadRootObjectBuilder>();

    // Register bindings
    builder.AddSingleton<IBinding, AutocadSendBinding>();

    // register send filters
    builder.AddTransient<ISendFilter, AutocadSelectionFilter>();

    // register send conversion cache
    builder.AddSingleton<ISendConversionCache, SendConversionCache>();
    builder.AddScoped<IInstanceObjectsManager<AutocadRootObject, List<Entity>>, AutocadInstanceObjectManager>();
  }

  public static void LoadReceive(SpeckleContainerBuilder builder)
  {
    // traversal
    builder.AddSingleton(DefaultTraversal.CreateTraversalFunc());

    // Object Builders
    builder.AddScoped<IHostObjectBuilder, AutocadHostObjectBuilder>();

    // Register bindings
    builder.AddSingleton<IBinding, AutocadReceiveBinding>();
  }
}

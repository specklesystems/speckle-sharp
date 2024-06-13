using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.PlugIns;
using Speckle.Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Rhino7.Bindings;
using Speckle.Connectors.Rhino7.Filters;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Interfaces;
using Speckle.Connectors.Rhino7.Operations.Send;
using Speckle.Connectors.Rhino7.Plugin;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Rhino7.Operations.Receive;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Rhino7.DependencyInjection;

public class RhinoConnectorModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    // Register instances initialised by Rhino
    builder.AddSingleton<PlugIn>(SpeckleConnectorsRhino7Plugin.Instance);
    builder.AddSingleton<Command>(SpeckleConnectorsRhino7Command.Instance);

    builder.AddAutofac();
    builder.AddConnectorUtils();
    builder.AddDUI();
    builder.AddDUIView();

    // POC: Overwriting the SyncToMainThread to SyncToCurrentThread for Rhino!
    builder.AddSingletonInstance<ISyncToThread, SyncToCurrentThread>();

    // Register other connector specific types
    builder.AddSingleton<IRhinoPlugin, RhinoPlugin>();
    builder.AddSingleton<DocumentModelStore, RhinoDocumentStore>();
    builder.AddSingleton<RhinoIdleManager>();

    // Register bindings
    builder.AddSingleton<IBinding, TestBinding>();
    builder.AddSingleton<IBinding, ConfigBinding>("connectorName", "Rhino"); // POC: Easier like this for now, should be cleaned up later
    builder.AddSingleton<IBinding, AccountBinding>();
    builder.AddSingleton<IBinding, RhinoBasicConnectorBinding>();
    builder.AddSingleton<IBasicConnectorBinding, RhinoBasicConnectorBinding>();
    builder.AddSingleton<IBinding, RhinoSelectionBinding>();
    builder.AddSingleton<IBinding, RhinoSendBinding>();
    builder.AddSingleton<IBinding, RhinoReceiveBinding>();

    // binding dependencies
    builder.AddTransient<CancellationManager>();

    // register send filters
    builder.AddScoped<ISendFilter, RhinoSelectionFilter>();
    builder.AddScoped<IHostObjectBuilder, RhinoHostObjectBuilder>();

    // register send conversion cache
    builder.AddSingleton<ISendConversionCache, SendConversionCache>();

    // register send operation and dependencies
    builder.AddScoped<SendOperation<RhinoObject>>();
    builder.AddSingleton(DefaultTraversal.CreateTraversalFunc());

    builder.AddScoped<IRootObjectBuilder<RhinoObject>, RhinoRootObjectBuilder>();
  }
}

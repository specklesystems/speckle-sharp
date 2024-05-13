using Autofac;
using Microsoft.Extensions.Logging;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.PlugIns;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.Rhino7.Bindings;
using Speckle.Connectors.Rhino7.Filters;
using Speckle.Connectors.Rhino7.HostApp;
using Speckle.Connectors.Rhino7.Interfaces;
using Speckle.Connectors.Rhino7.Operations.Send;
using Speckle.Connectors.Rhino7.Plugin;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Rhino7.Operations.Receive;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Rhino7.DependencyInjection;

public class AutofacRhinoModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    RegisterLoggerFactory(builder);

    // Register instances initialised by Rhino
    builder.RegisterInstance<PlugIn>(SpeckleConnectorsRhino7Plugin.Instance).SingleInstance();
    builder.RegisterInstance<Command>(SpeckleConnectorsRhino7Command.Instance).SingleInstance();

    // Register DUI3 related stuff
    builder.RegisterInstance(GetJsonSerializerSettings()).SingleInstance();
    builder.RegisterType<DUI3ControlWebView>().SingleInstance();
    builder.RegisterType<BrowserBridge>().As<IBridge>().InstancePerDependency(); // POC: Each binding should have it's own bridge instance

    // Register other connector specific types
    builder.RegisterType<RhinoPlugin>().As<IRhinoPlugin>().SingleInstance();
    builder
      .RegisterType<RhinoDocumentStore>()
      .As<DocumentModelStore>()
      .SingleInstance()
      .WithParameter("writeToFileOnChange", true);
    builder.RegisterType<RhinoIdleManager>().SingleInstance();

    // Register bindings
    builder.RegisterType<TestBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<ConfigBinding>().As<IBinding>().SingleInstance().WithParameter("connectorName", "Rhino"); // POC: Easier like this for now, should be cleaned up later
    builder.RegisterType<AccountBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RhinoBasicConnectorBinding>().As<IBinding>().As<IBasicConnectorBinding>().SingleInstance();
    builder.RegisterType<RhinoSelectionBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RhinoSendBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RhinoReceiveBinding>().As<IBinding>().SingleInstance();

    // binding dependencies
    builder.RegisterType<CancellationManager>().InstancePerDependency();
    builder.RegisterType<ServerTransport>().As<ITransport>().InstancePerDependency();

    // register send filters
    builder.RegisterType<RhinoSelectionFilter>().As<ISendFilter>().InstancePerDependency();

    // register send operation and dependencies
    builder.RegisterType<UnitOfWorkFactory>().As<IUnitOfWorkFactory>().InstancePerLifetimeScope();
    builder.RegisterType<SendOperation<RhinoObject>>().InstancePerLifetimeScope();
    builder.RegisterType<ReceiveOperation>().InstancePerLifetimeScope();
    builder.RegisterInstance(DefaultTraversal.CreateTraversalFunc());
    builder.RegisterType<SyncToCurrentThread>().As<ISyncToMainThread>().SingleInstance();

    builder.RegisterType<RhinoHostObjectBuilder>().As<IHostObjectBuilder>().InstancePerLifetimeScope();

    builder.RegisterType<RootObjectBuilder>().As<IRootObjectBuilder<RhinoObject>>().SingleInstance();
    builder.RegisterType<RootObjectSender>().As<IRootObjectSender>().SingleInstance();

    builder.RegisterType<ServerTransport>().As<ITransport>().InstancePerDependency();
  }

  private static JsonSerializerSettings GetJsonSerializerSettings()
  {
    // Register WebView2 panel stuff
    JsonSerializerSettings settings =
      new()
      {
        Error = (_, args) =>
        {
          // POC: we should probably do a bit more than just swallowing this!
          Console.WriteLine("*** JSON ERROR: " + args.ErrorContext);
        },
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        Converters = { new DiscriminatedObjectConverter(), new AbstractConverter<DiscriminatedObject, ISendFilter>() }
      };
    return settings;
  }

  private static void RegisterLoggerFactory(ContainerBuilder builder)
  {
    // POC: will likely need refactoring with our reporting pattern.
    var serilogLogger = new LoggerConfiguration().MinimumLevel
      .Debug()
      .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
      .CreateLogger();

    ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(serilogLogger);
    builder.RegisterInstance(loggerFactory).As<ILoggerFactory>().SingleInstance();
  }
}

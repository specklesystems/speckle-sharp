using Autofac;
using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.ArcGIS.Bindings;
using Speckle.Connectors.ArcGis.Operations.Send;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Utils;
using Speckle.Converters.Common;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;
using Speckle.Connectors.ArcGIS.Utils;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Converters.ArcGIS3;
using Speckle.Core.Transports;
using Speckle.Connectors.ArcGIS.Operations.Receive;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;
using ArcGIS.Core.Geometry;
using Speckle.Connectors.ArcGIS.Filters;
using Speckle.Connectors.DUI.Models;

// POC: This is a temp reference to root object senders to tweak CI failing after having generic interfaces into common project.
// This should go whenever it is aligned.
using IRootObjectSender = Speckle.Connectors.ArcGis.Operations.Send.IRootObjectSender;
using RootObjectSender = Speckle.Connectors.ArcGis.Operations.Send.RootObjectSender;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.ArcGIS.DependencyInjection;

public class AutofacArcGISModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    RegisterLoggerFactory(builder);

    // Register DUI3 related stuff
    builder.RegisterInstance(GetJsonSerializerSettings()).SingleInstance();
    builder.RegisterType<BrowserBridge>().As<IBridge>().InstancePerDependency(); //TODO: Verify why we need one bridge instance per dependency.

    builder.RegisterType<DUI3ControlWebView>().SingleInstance();
    // NOTE: I've scaffolded the auto write to file on change here, but the actual doc store is not implemented, so this does nothing i suspect.
    // The actual implementation is down to Kate :)
    builder
      .RegisterType<ArcGISDocumentStore>()
      .As<DocumentModelStore>()
      .SingleInstance()
      .WithParameter("writeToFileOnChange", true);

    // Register bindings
    builder.RegisterType<TestBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<ConfigBinding>().As<IBinding>().SingleInstance().WithParameter("connectorName", "ArcGIS"); // POC: Easier like this for now, should be cleaned up later
    builder.RegisterType<AccountBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<BasicConnectorBinding>().As<IBinding>().As<IBasicConnectorBinding>().SingleInstance();
    builder.RegisterType<ArcGISSelectionBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<ArcGISSendBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<ArcGISReceiveBinding>().As<IBinding>().SingleInstance();
    builder
      .RegisterType<ArcGISToSpeckleUnitConverter>()
      .As<IHostToSpeckleUnitConverter<Unit>>()
      .InstancePerLifetimeScope();

    // Operations
    builder.RegisterType<ReceiveOperation>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<SyncToCurrentThread>().As<ISyncToMainThread>().InstancePerLifetimeScope();

    // Object Builders
    builder.RegisterType<HostObjectBuilder>().As<IHostObjectBuilder>().InstancePerLifetimeScope();
    // POC: Register here also RootObjectBuilder as IRootObjectBuilder

    // binding dependencies
    builder.RegisterType<CancellationManager>().InstancePerDependency();

    // register send filters
    builder.RegisterType<ArcGISSelectionFilter>().As<ISendFilter>().InstancePerLifetimeScope();

    // register send operation and dependencies
    builder.RegisterType<SendOperation>().InstancePerLifetimeScope();
    builder.RegisterType<RootObjectBuilder>().InstancePerLifetimeScope();
    builder.RegisterType<RootObjectSender>().As<IRootObjectSender>().InstancePerLifetimeScope();
    builder.RegisterInstance(DefaultTraversal.CreateTraversalFunc());

    //POC: how tf does this work?
    builder.RegisterType<ServerTransport>().As<ITransport>().SingleInstance();

    // Register converter factory
    builder.RegisterType<UnitOfWorkFactory>().As<IUnitOfWorkFactory>().InstancePerLifetimeScope();
  }

  //poc: dupe code
  private static JsonSerializerSettings GetJsonSerializerSettings()
  {
    // Register WebView2 panel stuff
    JsonSerializerSettings settings =
      new()
      {
        Error = (_, args) =>
        {
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
    var serilogLogger = new LoggerConfiguration().MinimumLevel
      .Debug()
      .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
      .CreateLogger();

    ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(serilogLogger);
    builder.RegisterInstance(loggerFactory).As<ILoggerFactory>().SingleInstance();
  }
}

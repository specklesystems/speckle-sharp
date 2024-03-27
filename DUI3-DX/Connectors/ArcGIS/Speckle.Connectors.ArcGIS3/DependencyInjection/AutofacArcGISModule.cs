using Autofac;
using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.ArcGIS.Bindings;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Connectors.ArcGis.Operations.Send;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Utils;
using Speckle.Converters.Common;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;
using Speckle.Connectors.ArcGIS.Utils;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Core.Transports;

namespace Speckle.Connectors.ArcGIS.DependencyInjection;

public class AutofacArcGISModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    RegisterLoggerFactory(builder);

    // Register DUI3 related stuff
    builder.RegisterInstance(GetJsonSerializerSettings()).SingleInstance();
    builder.RegisterType<BrowserBridge>().As<IBridge>().InstancePerDependency(); //TODO: Verify why we need one bridge instance per dependency.

    builder.RegisterType<SpeckleDUI3>().SingleInstance();
    builder.RegisterType<ArcGISDocumentStore>().SingleInstance();

    // Register bindings
    builder.RegisterType<AccountBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<BasicConnectorBinding>().As<IBinding>().As<IBasicConnectorBinding>().SingleInstance();
    builder.RegisterType<ArcGISSendBinding>().As<IBinding>().SingleInstance();

    // binding dependencies
    builder.RegisterType<CancellationManager>().InstancePerDependency();
    builder.RegisterType<SendOperation>().InstancePerDependency();

    // register send operation and dependencies
    builder.RegisterType<SendOperation>().SingleInstance();
    builder.RegisterType<RootObjectBuilder>().SingleInstance();
    builder.RegisterType<RootObjectSender>().As<IRootObjectSender>().SingleInstance();

    //TODO: how tf does this work?
    builder.RegisterType<ServerTransport>().As<ITransport>().SingleInstance();

    // Register converter factory
    builder
      .RegisterType<ScopedFactory<ISpeckleConverterToSpeckle>>()
      .As<IScopedFactory<ISpeckleConverterToSpeckle>>()
      .InstancePerLifetimeScope();
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

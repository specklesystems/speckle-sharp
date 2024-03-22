using System;
using Autofac;
using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.Commands;
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
using Speckle.Converters.Common;
using Speckle.Converters.Rhino7;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Connectors.Rhino7.DependencyInjection;

public class AutofacRhinoModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    RegisterLoggerFactory(builder);

    // Register instances initialised by Rhino
    builder.RegisterInstance<PlugIn>(SpeckleConnectorsRhino7Plugin.Instance);
    builder.RegisterInstance<Command>(SpeckleConnectorsRhino7Command.Instance);

    // Register DUI3 related stuff
    builder.RegisterInstance(GetJsonSerializerSettings()).SingleInstance();
    builder.RegisterType<SpeckleRhinoPanel>().SingleInstance();
    builder.RegisterType<BrowserBridge>().As<IBridge>().InstancePerDependency(); // POC: Each binding should have it's own bridge instance

    // Register other connector specific types
    builder.RegisterType<RhinoPlugin>().As<IRhinoPlugin>().SingleInstance();
    builder.RegisterType<RhinoDocumentStore>().As<DocumentModelStore>().SingleInstance();
    builder.RegisterType<RhinoIdleManager>().SingleInstance();

    // Register bindings
    builder.RegisterType<AccountBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RhinoBasicConnectorBinding>().As<IBinding>().As<IBasicConnectorBinding>().SingleInstance();
    builder.RegisterType<RhinoSelectionBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RhinoSendBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RhinoToSpeckleUnitConverter>().As<IHostToSpeckleUnitConverter<UnitSystem>>().SingleInstance();

    // binding dependencies
    builder.RegisterType<CancellationManager>().InstancePerDependency();

    // register send filters
    builder.RegisterType<RhinoSelectionFilter>().As<ISendFilter>().InstancePerDependency();
    builder.RegisterType<RhinoEverythingFilter>().As<ISendFilter>().InstancePerDependency();

    // register send operation and dependencies
    builder.RegisterType<SendOperation>().SingleInstance();
    builder.RegisterType<RootObjectBuilder>().SingleInstance();
    builder.RegisterType<RootObjectSender>().As<IRootObjectSender>().SingleInstance();
    builder.RegisterType<ServerTransport>().As<ITransport>().InstancePerDependency();

    builder
      .RegisterType<ScopedFactory<ISpeckleConverterToSpeckle>>()
      .As<IScopedFactory<ISpeckleConverterToSpeckle>>()
      .InstancePerLifetimeScope();
  }

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

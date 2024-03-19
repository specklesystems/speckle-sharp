using System;
using Autofac;
using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.ArcGIS.Bindings;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Utils;
using Speckle.Converters.Common;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;
using Speckle.Connectors.ArcGIS.Utils;

namespace Speckle.Connectors.ArcGIS.DependencyInjetion;

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

    // Register converter factory
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

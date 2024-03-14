using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autofac;
using CefSharp;
using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.Revit.Bindings;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Converters.Common;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Connectors.Revit.DependencyInjection;

// POC: should interface out things that are not
class AutofacUIModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterInstance(new RevitContext());

    // POC: different versons for different versions of CEF
    builder.RegisterInstance<BindingOptions>(BindingOptions.DefaultBinder);

    // create JSON Settings
    // POC: this could be created through a factory or delegate, maybe delegate factory
    // https://autofac.readthedocs.io/en/latest/advanced/delegate-factories.html
    JsonSerializerSettings settings =
      new()
      {
        Error = (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) =>
        {
          Console.WriteLine("*** JSON ERROR: " + args.ErrorContext.ToString());
        },
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        Converters = { new DiscriminatedObjectConverter(), new AbstractConverter<DiscriminatedObject, ISendFilter>() }
      };

    var panel = new CefSharpPanel();
    panel.Browser.JavascriptObjectRepository.NameConverter = null;

    builder.RegisterInstance(panel).SingleInstance();
    builder.RegisterInstance(settings).SingleInstance();
    builder.RegisterType<RevitPlugin>().As<IRevitPlugin>().SingleInstance();
    builder.RegisterType<BrowserBridge>().As<IBridge>().InstancePerDependency();

    // register UI bindings
    builder.RegisterType<AccountBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<BasicConnectorBindingRevit>().As<IBinding>().SingleInstance();
    builder.RegisterType<SelectionBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<SendBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<ReceiveBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RevitIdleManager>().As<IRevitIdleManager>().SingleInstance();

    // register
    builder.RegisterType<RevitDocumentStore>().SingleInstance();

    builder
      .RegisterType<ScopedFactory<ISpeckleConverterToSpeckle>>()
      .As<IScopedFactory<ISpeckleConverterToSpeckle>>()
      .InstancePerLifetimeScope();

    // POC: logging factory couldn't be added, which is the recommendation, due to janky dependencies
    // having a SpeckleLogging service, might be interesting, if a service can listen on a local port or use named pipes
    var current = Directory.GetCurrentDirectory();
    var serilogLogger = new LoggerConfiguration().MinimumLevel
      .Debug()
      .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
      .CreateLogger();

    ILoggerFactory loggerFactory = new LoggerFactory();
    var serilog = loggerFactory.AddSerilog(serilogLogger);
    builder.RegisterInstance(loggerFactory).As<ILoggerFactory>().SingleInstance();
  }
}

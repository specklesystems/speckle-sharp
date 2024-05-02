using System;
using System.IO;
using Autofac;
using CefSharp;
using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.Revit.Bindings;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Operations.Send;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;
using IRootObjectSender = Speckle.Connectors.Revit.Operations.Send.IRootObjectSender;
using RootObjectSender = Speckle.Connectors.Revit.Operations.Send.RootObjectSender;

namespace Speckle.Connectors.Revit.DependencyInjection;

// POC: should interface out things that are not
public class AutofacUIModule : Module
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

    // Storage Schema
    builder.RegisterType<DocumentModelStorageSchema>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<IdStorageSchema>().AsSelf().InstancePerLifetimeScope();

    // POC: we need to review the scopes and create a document on what the policy is
    // and where the UoW should be
    // register UI bindings
    builder.RegisterType<TestBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<ConfigBinding>().As<IBinding>().SingleInstance().WithParameter("connectorName", "Revit"); // POC: Easier like this for now, should be cleaned up later
    builder.RegisterType<AccountBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<BasicConnectorBindingRevit>().As<IBinding>().SingleInstance();
    builder.RegisterType<SelectionBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<SendBinding>().As<IBinding>().SingleInstance();
    // POC: Hide Load button as Revit connector is publish only for the open alpha.
    //builder.RegisterType<ReceiveBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<RevitIdleManager>().As<IRevitIdleManager>().SingleInstance();

    // send operation and dependencies
    builder.RegisterType<SyncToUIThread>().As<ISyncToMainThread>().SingleInstance().AutoActivate();
    builder.RegisterType<SendOperation>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<SendSelection>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<ToSpeckleConvertedObjectsCache>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<RootObjectBuilder>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<ServerTransport>().As<ITransport>().InstancePerDependency();
    builder.RegisterType<RootObjectSender>().As<IRootObjectSender>().SingleInstance();

    // register
    builder.RegisterType<RevitDocumentStore>().As<DocumentModelStore>().SingleInstance().AsSelf();

    // POC: this can be injected in maybe a common place, perhaps a module in Speckle.Converters.Common.DependencyInjection
    builder.RegisterType<UnitOfWorkFactory>().As<IUnitOfWorkFactory>().InstancePerLifetimeScope();

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

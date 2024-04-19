using Autodesk.AutoCAD.DatabaseServices;
using Autofac;
using Microsoft.Extensions.Logging;
using Serilog;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.Bindings;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.Plugin;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Autocad;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;
using Speckle.Connectors.Autocad.Interfaces;
using Speckle.Connectors.Utils.Cancellation;
using Speckle.Connectors.Autocad.Filters;
using Speckle.Connectors.Autocad.Operations.Receive;
using Speckle.Connectors.DUI.Models.Card.SendFilter;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Autocad.DependencyInjection;

public class AutofacAutocadModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    RegisterLoggerFactory(builder);

    // Register DUI3 related stuff
    builder.RegisterInstance(GetJsonSerializerSettings()).SingleInstance();
    builder.RegisterType<Dui3PanelWebView>().SingleInstance();
    builder.RegisterType<BrowserBridge>().As<IBridge>().InstancePerDependency(); // POC: Each binding should have it's own bridge instance

    // Register other connector specific types
    builder.RegisterType<AutocadPlugin>().As<IAutocadPlugin>().SingleInstance();
    builder.RegisterType<TransactionContext>().InstancePerDependency();
    builder.RegisterInstance<AutocadDocumentManager>(new AutocadDocumentManager()); // TODO: Dependent to TransactionContext, can be moved to AutocadContext
    builder.RegisterType<AutocadDocumentStore>().As<DocumentModelStore>().SingleInstance();
    builder.RegisterType<AutocadContext>().SingleInstance();
    builder.RegisterType<AutocadLayerManager>().SingleInstance();
    builder.RegisterType<AutocadIdleManager>().SingleInstance();

    // Operations
    builder.RegisterType<ReceiveOperation>().AsSelf().SingleInstance();
    builder.RegisterType<SyncToUIThread>().As<ISyncToMainThread>().SingleInstance();

    // Object Builders
    builder.RegisterType<HostObjectBuilder>().As<IHostObjectBuilder>().InstancePerDependency();
    // POC: Register here also RootObjectBuilder as IRootObjectBuilder

    // Register bindings
    builder.RegisterType<TestBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<ConfigBinding>().As<IBinding>().SingleInstance().WithParameter("connectorName", "Autocad"); // POC: Easier like this for now, should be cleaned up later
    builder.RegisterType<AccountBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<AutocadBasicConnectorBinding>().As<IBinding>().As<IBasicConnectorBinding>().SingleInstance();
    builder.RegisterType<AutocadSelectionBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<AutocadSendBinding>().As<IBinding>().SingleInstance();
    builder.RegisterType<AutocadReceiveBinding>().As<IBinding>().SingleInstance();
    builder
      .RegisterType<AutocadToSpeckleUnitConverter>()
      .As<IHostToSpeckleUnitConverter<UnitsValue>>()
      .SingleInstance();

    // binding dependencies
    builder.RegisterType<CancellationManager>().InstancePerDependency();

    // register send filters
    builder.RegisterType<AutocadSelectionFilter>().As<ISendFilter>().InstancePerDependency();

    // Register converter factory
    builder.RegisterType<UnitOfWorkFactory>().As<IUnitOfWorkFactory>().InstancePerLifetimeScope();
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

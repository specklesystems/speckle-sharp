using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using CefSharp;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Utils;
using Speckle.Connectors.Revit.Bindings;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Plugin;
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

    // register my types
    builder.RegisterType<RevitPlugin>().As<IRevitPlugin>().SingleInstance();

    // create JSON Settings
    JsonSerializerSettings settings =
      new()
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        Converters = { new DiscriminatedObjectConverter(), new AbstractConverter<DiscriminatedObject, ISendFilter>() }
      };

    var panel = new CefSharpPanel();
    panel.Browser.JavascriptObjectRepository.NameConverter = null;
    var browserSender = new BrowserSender(settings, panel.Browser.ExecuteScriptAsync);
    builder.RegisterInstance(panel).SingleInstance();

    // POC: panel static is a bit meh :D
    builder.RegisterInstance(browserSender).As<IBrowserSender>().SingleInstance();

    builder.RegisterInstance(settings).SingleInstance();
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
  }
}
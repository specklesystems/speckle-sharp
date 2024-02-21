using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Speckle.ConnectorRevitDUI3.Bindings;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.Revit.Plugin;

namespace Speckle.Connectors.Revit.DependencyInjection;

class AutofacUIModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // register my types
    builder.RegisterType<RevitPlugin>().As<IRevitPlugin>().SingleInstance();

    // register UI bindings
    builder.RegisterType<IBinding>().As<ConfigBinding>().SingleInstance();
    builder.RegisterType<IBinding>().As<AccountBinding>().SingleInstance();
    builder.RegisterType<IBinding>().As<BasicConnectorBindingRevit>().SingleInstance();
    builder.RegisterType<IBinding>().As<SelectionBinding>().SingleInstance();
    builder.RegisterType<IBinding>().As<SendBinding>().SingleInstance();
    builder.RegisterType<IBinding>().As<ReceiveBinding>().SingleInstance();
  }
}

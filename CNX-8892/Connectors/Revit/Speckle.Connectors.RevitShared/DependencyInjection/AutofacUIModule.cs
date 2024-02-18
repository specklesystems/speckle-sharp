using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Speckle.Connectors.Revit.Plugin;

namespace Speckle.Connectors.Revit.DependencyInjection
{
  class AutofacUIModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      // register my types
      builder.RegisterType<RevitPlugin>().As<IRevitPlugin>().SingleInstance();

      // register UI bindings
    }
  }
}

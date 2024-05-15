﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Autocad;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection.ToHost;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad2023.DependencyInjection;

public class AutofacAutocadConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // POC: below comment maybe incorrect (sorry if I wrote that!) stateless services
    // can be injected as Singleton(), only where we have state we wish to wrap in a unit of work
    builder.RegisterType<AutocadConverterToSpeckle>().As<ISpeckleConverterToSpeckle>().InstancePerLifetimeScope();
    builder.RegisterType<ToHostConverterWithFallback>().As<ISpeckleConverterToHost>().InstancePerLifetimeScope();

    // single stack per conversion
    builder
      .RegisterType<AutocadConversionContextStack>()
      .As<IConversionContextStack<Document, UnitsValue>>()
      .InstancePerLifetimeScope();

    // factory for conversions
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion>>()
      .InstancePerLifetimeScope();

    builder
      .RegisterType<Factory<string, ISpeckleObjectToHostConversion>>()
      .As<IFactory<string, ISpeckleObjectToHostConversion>>()
      .InstancePerLifetimeScope();
    builder
      .RegisterType<RecursiveConverterResolver<ISpeckleObjectToHostConversion>>()
      .As<IConverterResolver<ISpeckleObjectToHostConversion>>()
      .InstancePerLifetimeScope();
  }
}
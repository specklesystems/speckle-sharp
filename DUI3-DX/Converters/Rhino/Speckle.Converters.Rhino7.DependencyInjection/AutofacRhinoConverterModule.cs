using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Rhino;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class AutofacRhinoConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // POC: below comment maybe incorrect (sorry if I wrote that!) stateless services
    // can be injected as Singleton(), only where we have state we wish to wrap in a unit of work
    // should be InstancePerLifetimeScope
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.RegisterType<RhinoConverterToSpeckle>().As<ISpeckleConverterToSpeckle>().SingleInstance();

    // single stack per conversion
    builder
      .RegisterType<RhinoConversionContextStack>()
      .As<IConversionContextStack<RhinoDoc, UnitSystem>>()
      .InstancePerLifetimeScope();

    // factory for conversions
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion>>();
  }
}

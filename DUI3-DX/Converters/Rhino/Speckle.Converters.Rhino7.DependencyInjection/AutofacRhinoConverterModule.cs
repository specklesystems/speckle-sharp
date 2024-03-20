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
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.RegisterType<RhinoConverterToSpeckle>().As<ISpeckleConverterToSpeckle>();

    builder
      .RegisterType<RhinoConversionContext>()
      .As<IConversionContext<RhinoDoc, UnitSystem>>()
      .InstancePerLifetimeScope();

    // factory for conversions
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion>>();
  }
}

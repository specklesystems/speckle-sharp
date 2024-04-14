using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Rhino;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Converters.Rhino7.ToHost;
using Speckle.Converters.Rhino7.ToSpeckle;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class AutofacRhinoConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // POC: below comment maybe incorrect (sorry if I wrote that!) stateless services
    // can be injected as Singleton(), only where we have state we wish to wrap in a unit of work
    // should be InstancePerLifetimeScope
    // most things should be InstancePerLifetimeScope so we get one per operation

    // single stack per conversion
    builder
      .RegisterType<RhinoConversionContextStack>()
      .As<IConversionContextStack<RhinoDoc, UnitSystem>>()
      .InstancePerLifetimeScope();

    builder.RegisterRawConversions();

    // To Speckle
    builder.RegisterType<RhinoToSpeckleUnitConverter>().As<IHostToSpeckleUnitConverter<UnitSystem>>().SingleInstance();
    builder.RegisterType<RhinoConverterToSpeckle>().As<ISpeckleConverterToSpeckle>().SingleInstance();
    builder
      .RegisterType<ScopedFactory<ISpeckleConverterToSpeckle>>()
      .As<IScopedFactory<ISpeckleConverterToSpeckle>>()
      .InstancePerLifetimeScope();

    /*
      POC: Moved the Injection of converters into the converter module. Not sure if this is 100% right, as this doesn't just register the conversions within this converter, but any conversions found in any Speckle.*.dll file.
      This will require consolidating across other connectors.
    */
    builder.InjectNamedTypes<IHostObjectToSpeckleConversion>();
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion>>();

    // To Host
    // POC: Missing Unit converter
    builder.RegisterType<RhinoConverterToHost>().As<ISpeckleConverterToHost>().SingleInstance();
    builder
      .RegisterType<ScopedFactory<ISpeckleConverterToHost>>()
      .As<IScopedFactory<ISpeckleConverterToHost>>()
      .InstancePerLifetimeScope();

    /*
      POC: Moved the Injection of converters into the converter module. Not sure if this is 100% right, as this doesn't just register the conversions within this converter, but any conversions found in any Speckle.*.dll file.
      This will require consolidating across other connectors.
    */
    builder.InjectNamedTypes<ISpeckleObjectToHostConversion>();
    builder
      .RegisterType<Factory<string, ISpeckleObjectToHostConversion>>()
      .As<IFactory<string, ISpeckleObjectToHostConversion>>();
  }
}

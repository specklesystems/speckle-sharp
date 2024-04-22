using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Rhino;
using Rhino.Runtime;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Converters.Rhino7.ToHost;
using Speckle.Converters.Rhino7.ToSpeckle;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class AutofacRhinoConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // single stack per conversion
    builder
      .RegisterType<RhinoConversionContextStack>()
      .As<IConversionContextStack<RhinoDoc, UnitSystem>>()
      .InstancePerLifetimeScope();

    builder.RegisterRawConversions();

    // To Speckle
    builder
      .RegisterType<RhinoToSpeckleUnitConverter>()
      .As<IHostToSpeckleUnitConverter<UnitSystem>>()
      .InstancePerLifetimeScope();
    builder
      .RegisterType<RhinoConverterToSpeckle>()
      .As<ISpeckleConverterToSpeckle<CommonObject>>()
      .InstancePerLifetimeScope();

    /*
      POC: CNX-9267 Moved the Injection of converters into the converter module. Not sure if this is 100% right, as this doesn't just register the conversions within this converter, but any conversions found in any Speckle.*.dll file.
      This will require consolidating across other connectors.
    */
    builder.InjectNamedTypes<IHostObjectToSpeckleConversion<CommonObject>>();
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion<CommonObject>>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion<CommonObject>>>()
      .InstancePerLifetimeScope();

    builder.RegisterType<RhinoConverterToHost>().As<ISpeckleConverterToHost<CommonObject>>().InstancePerLifetimeScope();

    /*
      POC: CNX-9267 Moved the Injection of converters into the converter module. Not sure if this is 100% right, as this doesn't just register the conversions within this converter, but any conversions found in any Speckle.*.dll file.
      This will require consolidating across other connectors.
    */
    builder.InjectNamedTypes<ISpeckleObjectToHostConversion<CommonObject>>();
    builder
      .RegisterType<Factory<string, ISpeckleObjectToHostConversion<CommonObject>>>()
      .As<IFactory<string, ISpeckleObjectToHostConversion<CommonObject>>>()
      .InstancePerLifetimeScope();
  }
}

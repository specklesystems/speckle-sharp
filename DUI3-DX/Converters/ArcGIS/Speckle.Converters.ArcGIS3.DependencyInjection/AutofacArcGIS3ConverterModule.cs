// POC: not sure we should have this here as it attaches us to autofac, maybe a bit prematurely...

using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.DependencyInjection;

public class AutofacArcGISConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.RegisterType<ArcGISConverterToSpeckle>().As<ISpeckleConverterToSpeckle>().InstancePerLifetimeScope();
    builder.RegisterType<ArcGISConverterToHost>().As<ISpeckleConverterToHost>().InstancePerLifetimeScope();
    builder.RegisterType<FeatureClassUtils>().As<IFeatureClassUtils>().InstancePerLifetimeScope();
    builder.RegisterType<ArcGISProjectUtils>().As<IArcGISProjectUtils>().InstancePerLifetimeScope();

    builder
      .RegisterType<ArcGISToSpeckleUnitConverter>()
      .As<IHostToSpeckleUnitConverter<Unit>>()
      .InstancePerLifetimeScope();

    // single stack per conversion
    builder
      .RegisterType<ArcGISConversionContextStack>()
      .As<IConversionContextStack<Map, Unit>>()
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
  }
}

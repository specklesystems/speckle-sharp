// POC: not sure we should have this here as it attaches us to autofac, maybe a bit prematurely...

using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.DependencyInjection;

public class AutofacArcGISConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.RegisterType<ArcGISConverterToSpeckle>().As<ISpeckleConverterToSpeckle>().SingleInstance();

    // single stack per conversion
    builder
      .RegisterType<ArcGISConversionContextStack>()
      .As<IConversionContextStack<Map, Unit>>()
      .InstancePerDependency();

    // factory for conversions
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion>>();
  }
}

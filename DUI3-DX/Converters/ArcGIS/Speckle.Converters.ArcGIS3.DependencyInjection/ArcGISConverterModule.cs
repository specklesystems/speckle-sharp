// POC: not sure we should have this here as it attaches us to autofac, maybe a bit prematurely...
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.DependencyInjection;

public class ArcGISConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddConverterCommon();
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.AddScoped<ISpeckleConverterToSpeckle, ArcGISConverterToSpeckle>();
    builder.AddScoped<ISpeckleConverterToHost, ArcGISConverterToHost>();
    builder.AddScoped<IFeatureClassUtils, FeatureClassUtils>();
    builder.AddScoped<IArcGISFieldUtils, ArcGISFieldUtils>();
    builder.AddScoped<ICharacterCleaner, CharacterCleaner>();
    builder.AddScoped<IArcGISProjectUtils, ArcGISProjectUtils>();
    builder.AddScoped<INonNativeFeaturesUtils, NonNativeFeaturesUtils>();

    builder.AddScoped<IHostToSpeckleUnitConverter<Unit>, ArcGISToSpeckleUnitConverter>();

    // single stack per conversion
    builder.AddScoped<IConversionContextStack<Map, Unit>, ArcGISConversionContextStack>();

    // factory for conversions
    builder.AddScoped<
      IFactory<string, IHostObjectToSpeckleConversion>,
      Factory<string, IHostObjectToSpeckleConversion>
    >();
    builder.AddScoped<
      IConverterResolver<IHostObjectToSpeckleConversion>,
      RecursiveConverterResolver<IHostObjectToSpeckleConversion>
    >();
  }
}

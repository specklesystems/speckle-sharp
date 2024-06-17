using ArcGIS.Core.Geometry;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;

namespace Speckle.Converters.ArcGIS3.DependencyInjection;

public class ArcGISConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    //don't need a host specific RootToSpeckleConverter
    builder.AddConverterCommon<LegacyRootToSpeckleConverter, ArcGISToSpeckleUnitConverter, Unit>();
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.AddScoped<IFeatureClassUtils, FeatureClassUtils>();
    builder.AddScoped<IArcGISFieldUtils, ArcGISFieldUtils>();
    builder.AddScoped<ICharacterCleaner, CharacterCleaner>();
    builder.AddScoped<INonNativeFeaturesUtils, NonNativeFeaturesUtils>();
    builder.AddScoped<ArcGISDocument>();

    builder.AddScoped<IHostToSpeckleUnitConverter<Unit>, ArcGISToSpeckleUnitConverter>();

    // single stack per conversion
    builder.AddScoped<IConversionContextStack<ArcGISDocument, Unit>, ArcGISConversionContextStack>();
  }
}

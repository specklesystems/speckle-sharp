using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Rhino;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Converters.Common.DependencyInjection.ToHost;
using Speckle.Converters.Rhino7.ToSpeckle;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class RhinoConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddConverterCommon();
    // single stack per conversion
    builder.AddScoped<IConversionContextStack<RhinoDoc, UnitSystem>, RhinoConversionContextStack>();

    // To Speckle
    builder.AddScoped<IHostToSpeckleUnitConverter<UnitSystem>, RhinoToSpeckleUnitConverter>();
    builder.AddScoped<ISpeckleConverterToSpeckle, RhinoConverterToSpeckle>();

    /*
      POC: CNX-9267 Moved the Injection of converters into the converter module. Not sure if this is 100% right, as this doesn't just register the conversions within this converter, but any conversions found in any Speckle.*.dll file.
      This will require consolidating across other connectors.
    */
    builder.AddScoped<
      IFactory<string, IHostObjectToSpeckleConversion>,
      Factory<string, IHostObjectToSpeckleConversion>
    >();
    builder.AddScoped<
      IConverterResolver<IHostObjectToSpeckleConversion>,
      RecursiveConverterResolver<IHostObjectToSpeckleConversion>
    >();
    builder.AddScoped<ISpeckleConverterToHost, ToHostConverterWithFallback>();
  }
}

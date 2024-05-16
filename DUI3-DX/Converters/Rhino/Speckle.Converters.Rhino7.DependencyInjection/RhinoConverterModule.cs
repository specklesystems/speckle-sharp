using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Rhino;
using Speckle.Converters.Common.DependencyInjection;
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
  }
}

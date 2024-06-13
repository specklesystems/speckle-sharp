using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Rhino;
using Speckle.Converters.Common.DependencyInjection;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class RhinoConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddConverterCommon<IRootToSpeckleConverter, RhinoToSpeckleUnitConverter, UnitSystem>();
    // single stack per conversion
    builder.AddScoped<IConversionContextStack<RhinoDoc, UnitSystem>, RhinoConversionContextStack>();
  }
}

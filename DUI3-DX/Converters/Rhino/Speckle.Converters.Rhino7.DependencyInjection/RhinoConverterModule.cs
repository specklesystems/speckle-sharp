using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class RhinoConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddConverterCommon<RootToSpeckleConverter, RhinoToSpeckleUnitConverter, RhinoUnitSystem>();
    // single stack per conversion
    builder.AddScoped<IConversionContextStack<IRhinoDoc, RhinoUnitSystem>, RhinoConversionContextStack>();
    builder.AddScoped<IRootElementProvider, RhinoRootElementProvider>();
  }
}

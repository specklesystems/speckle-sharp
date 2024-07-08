using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Rhino;
using Speckle.Converters.Common.DependencyInjection;

namespace Speckle.Converters.Rhino7.DependencyInjection;

public class RhinoConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    // Register single root
    builder.AddRootCommon<RootToSpeckleConverter>();

    // register all application converters and context stacks
    builder.AddApplicationConverters<RhinoToSpeckleUnitConverter, UnitSystem>();
    builder.AddScoped<IConversionContextStack<RhinoDoc, UnitSystem>, RhinoConversionContextStack>();
  }
}

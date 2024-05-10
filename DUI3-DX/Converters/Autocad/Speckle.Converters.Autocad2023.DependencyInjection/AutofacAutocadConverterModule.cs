using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Autocad;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection.ToHost;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad2023.DependencyInjection;

public class AutocadConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    // POC: below comment maybe incorrect (sorry if I wrote that!) stateless services
    // can be injected as Singleton(), only where we have state we wish to wrap in a unit of work
    builder.AddScoped<ISpeckleConverterToSpeckle, AutocadConverterToSpeckle>();
    builder.AddScoped<ISpeckleConverterToHost, ToHostConverterWithFallback>();

    // single stack per conversion
    builder.AddScoped<IConversionContextStack<Document, UnitsValue>, AutocadConversionContextStack>();

    // factory for conversions
    builder.AddScoped<
      IFactory<string, IHostObjectToSpeckleConversion>,
      Factory<string, IHostObjectToSpeckleConversion>
    >();
    builder.AddScoped<
      IFactory<string, ISpeckleObjectToHostConversion>,
      Factory<string, ISpeckleObjectToHostConversion>
    >();
    builder.AddScoped<
      IConverterResolver<ISpeckleObjectToHostConversion>,
      RecursiveConverterResolver<ISpeckleObjectToHostConversion>
    >();
  }
}

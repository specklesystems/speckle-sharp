using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.DependencyInjection.ToHost;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

public static class ContainerRegistration
{
  public static void AddConverterCommon(this SpeckleContainerBuilder builder)
  {
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

    builder.AddScoped<
      IFactory<string, ISpeckleObjectToHostConversion>,
      Factory<string, ISpeckleObjectToHostConversion>
    >();
    builder.AddScoped<
      IConverterResolver<ISpeckleObjectToHostConversion>,
      RecursiveConverterResolver<ISpeckleObjectToHostConversion>
    >();

    builder.AddScoped<ISpeckleConverterToHost, ToHostConverterWithFallback>();

    builder.RegisterRawConversions();
    builder.InjectNamedTypes<IHostObjectToSpeckleConversion>();
    builder.InjectNamedTypes<ISpeckleObjectToHostConversion>();
  }
}

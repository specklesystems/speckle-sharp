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
    builder.AddScoped<IFactory<IToSpeckleTopLevelConverter>, Factory<IToSpeckleTopLevelConverter>>();
    builder.AddScoped<
      IConverterResolver<IToSpeckleTopLevelConverter>,
      RecursiveConverterResolver<IToSpeckleTopLevelConverter>
    >();

    builder.AddScoped<IFactory<IToHostTopLevelConverter>, Factory<IToHostTopLevelConverter>>();
    builder.AddScoped<
      IConverterResolver<IToHostTopLevelConverter>,
      RecursiveConverterResolver<IToHostTopLevelConverter>
    >();

    builder.AddScoped<IRootToHostConverter, ConverterWithFallback>();

    builder.RegisterRawConversions();
    builder.InjectNamedTypes<IToSpeckleTopLevelConverter>();
    builder.InjectNamedTypes<IToHostTopLevelConverter>();
  }
}

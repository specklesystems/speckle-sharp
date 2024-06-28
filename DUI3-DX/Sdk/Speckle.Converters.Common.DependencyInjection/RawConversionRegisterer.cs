using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

// POC: review and see if it can be made more generic, related to the
// NameAndRankAttribute work that needs doing
public static class ConverterRegistration
{
  public static void RegisterConverters(this SpeckleContainerBuilder containerBuilder)
  {
    // POC: hard-coding speckle... :/
    foreach (Type speckleType in containerBuilder.SpeckleTypes)
    {
      RegisterRawConversionsForType(containerBuilder.ContainerBuilder, speckleType);
    }
  }

  private static void RegisterRawConversionsForType(ContainerBuilder containerBuilder, Type type)
  {
    if (!type.IsClass || type.IsAbstract)
    {
      return;
    }

    var rawConversionInterfaces = type.GetInterfaces()
      .Where(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(ITypedConverter<,>));

    foreach (var conversionInterface in rawConversionInterfaces)
    {
      containerBuilder
        .RegisterType(type)
        .As(conversionInterface)
        .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
        .InstancePerLifetimeScope();
    }
  }
}

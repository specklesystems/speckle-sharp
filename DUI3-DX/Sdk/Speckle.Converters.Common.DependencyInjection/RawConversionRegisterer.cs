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
      foreach (var conversionInterface in RegisterConversionsForType(speckleType))
      {
        containerBuilder.ContainerBuilder
          .RegisterType(speckleType)
          .As(conversionInterface)
          .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
          .InstancePerLifetimeScope();
      }
    }
  }

  private static IEnumerable<Type> RegisterConversionsForType(Type type)
  {
    if (!type.IsClass || type.IsAbstract)
    {
      yield break;
    }

    var rawConversionInterfaces = type.GetInterfaces()
      .Where(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(ITypedConverter<,>));

    foreach (var conversionInterface in rawConversionInterfaces)
    {
      yield return conversionInterface.NotNull();
    }
  }
}

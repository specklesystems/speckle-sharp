using System.Reflection;
using Autofac;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

// POC: review and see if it can be made more generic, related to the
// NameAndRankAttribute work that needs doing
public static class RawConversionRegisterer
{
  public static ContainerBuilder RegisterRawConversions(this ContainerBuilder containerBuilder)
  {
    // POC: hard-coding speckle... :/
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("Speckle", StringComparison.CurrentCultureIgnoreCase)))
    {
      foreach (var type in asm.GetTypes())
      {
        RegisterRawConversionsForType(containerBuilder, type);
      }
    }

    return containerBuilder;
  }

  private static void RegisterRawConversionsForType(ContainerBuilder containerBuilder, Type type)
  {
    if (!type.IsClass || type.IsAbstract)
    {
      return;
    }

    var rawConversionInterfaces = type.GetInterfaces()
      .Where(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IRawConversion<,>));

    foreach (var conversionInterface in rawConversionInterfaces)
    {
      containerBuilder
        .RegisterType(type)
        .As(conversionInterface)
        .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
        .InstancePerLifetimeScope();
    }
  }

  public static ContainerBuilder RegisterTypesInAssemblyAsInterface(
    this ContainerBuilder containerBuilder,
    Assembly assembly,
    Type interfaceType
  )
  {
    bool isGeneric = interfaceType.IsGenericType;
    foreach (var type in assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract))
    {
      IEnumerable<Type> interfaceTypes = type.GetInterfaces();
      interfaceTypes = isGeneric
        ? interfaceTypes.Where(it => it.IsGenericType && it.GetGenericTypeDefinition() == interfaceType)
        : interfaceTypes.Where(it => it == interfaceType);

      foreach (var iterfaceTypeMatch in interfaceTypes)
      {
        containerBuilder.RegisterType(type).As(iterfaceTypeMatch).InstancePerLifetimeScope();
      }
    }

    return containerBuilder;
  }
}

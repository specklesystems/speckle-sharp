using Autofac;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

public static class RawConversionRegisterer
{
  public static ContainerBuilder RegisterRawConversions(this ContainerBuilder containerBuilder)
  {
    // POC: hard-coding speckle... :/
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("Speckle")))
    {
      foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
      {
        if (GetImplementedRawConversionType(type) is not Type interfaceType)
        {
          continue;
        }

        Type firstGenericType = interfaceType.GenericTypeArguments[0];
        containerBuilder
          .RegisterType(type)
          .AsImplementedInterfaces()
          .Keyed<IHostObjectToSpeckleConversion>(firstGenericType)
          .InstancePerLifetimeScope();
      }
    }

    return containerBuilder;
  }

  public static Type? GetImplementedRawConversionType(Type givenType)
  {
    foreach (var it in givenType.GetInterfaces())
    {
      if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IRawConversion<,>))
      {
        return it;
      }
    }

    return null;
  }
}

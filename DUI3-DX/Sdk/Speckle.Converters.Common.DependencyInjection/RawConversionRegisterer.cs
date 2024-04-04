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
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("Speckle")))
    {
      foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
      {
        if (GetImplementedRawConversionType(type) is not Type interfaceType)
        {
          continue;
        }

        var registrationBuilder = containerBuilder.RegisterType(type).As(interfaceType);

        Type firstGenericType = interfaceType.GenericTypeArguments[0];
        var singleParamRawConversionType = typeof(IRawConversion<>).MakeGenericType(firstGenericType);
        if (singleParamRawConversionType.IsAssignableFrom(type))
        {
          registrationBuilder = registrationBuilder.As(singleParamRawConversionType);
        }

        if (typeof(IHostObjectToSpeckleConversion).IsAssignableFrom(type))
        {
          registrationBuilder = registrationBuilder
            .As<IHostObjectToSpeckleConversion>()
            .Keyed<IHostObjectToSpeckleConversion>(firstGenericType);
        }

        registrationBuilder.InstancePerLifetimeScope();
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

using System.Reflection;
using Autofac;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;

namespace Speckle.Converters.Common.DependencyInjection;

public static class ConversionTypesInjector
{
  public static ContainerBuilder InjectNamedTypes<T>(this ContainerBuilder containerBuilder)
  {
    List<Type> types = new();

    // POC: hard-coding speckle... :/
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("Speckle")))
    {
      try
      {
        var asmTypes = asm.GetTypes();

        // POC: IsAssignableFrom()
        types.AddRange(asmTypes.Where(y => y.GetInterface(typeof(T).Name) != null));
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        // POC: be more specific
      }
    }

    // we only care about named types
    var byName = types
      .Where(x => x.GetCustomAttribute<NameAndRankValueAttribute>() != null)
      .Select(x =>
      {
        var nameAndRank = x.GetCustomAttribute<NameAndRankValueAttribute>();

        return (name: nameAndRank.Name, rank: nameAndRank.Rank, type: x);
      })
      .ToList();

    // we'll register the types accordingly
    var names = byName.Select(x => x.name).Distinct();
    foreach (string name in names)
    {
      var namedTypes = byName.Where(x => x.name == name).OrderByDescending(y => y.rank).ToList();

      // first type found
      var first = namedTypes[0];

      // POC: may need to be instance per lifecycle scope
      containerBuilder.RegisterType(first.type).Keyed<T>(first.name).InstancePerLifetimeScope();

      // POC: not sure yet if...
      // * This should be an array of types
      // * Whether the scope should be modified or modifiable
      // * Whether this is in the write project... hmmm
      // POC: IsAssignableFrom()
      var secondaryType = first.type.GetInterface(typeof(IRawConversion<,>).Name);
      // POC: should we explode if no found?
      if (secondaryType != null)
      {
        containerBuilder
          .RegisterType(first.type)
          .As(secondaryType)
          .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
          .InstancePerLifetimeScope();
      }

      // register subsequent types with rank
      namedTypes.RemoveAt(0);
      foreach (var other in namedTypes)
      {
        // POC: is this the right scope?
        containerBuilder.RegisterType(other.type).Keyed<T>($"{other.name}|{other.rank}").InstancePerLifetimeScope();

        // POC: not sure yet if...
        // * This should be an array of types
        // * Whether the scope should be modified or modifiable
        // * Whether this is in the write project... hmmm
        // POC: IsAssignableFrom()
        // NOT very DRY
        secondaryType = secondaryType = first.type.GetInterface(typeof(IRawConversion<,>).Name);
        // POC: should we explode if no found?
        if (secondaryType != null)
        {
          containerBuilder.RegisterType(first.type).As(secondaryType).InstancePerLifetimeScope();
        }
      }
    }

    return containerBuilder;
  }
}

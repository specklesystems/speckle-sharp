using System.Reflection;
using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

public static class ConversionTypesInjector
{
  public static void InjectNamedTypes<T>(this SpeckleContainerBuilder containerBuilder)
    where T : notnull
  {
    var types = containerBuilder.SpeckleTypes;

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
      containerBuilder.ContainerBuilder.RegisterType(first.type).Keyed<T>(first.name).InstancePerLifetimeScope();

      // POC: not sure yet if...
      // * This should be an array of types
      // * Whether the scope should be modified or modifiable
      // * Whether this is in the write project... hmmm
      // POC: IsAssignableFrom()
      var secondaryType = first.type.GetInterface(typeof(IRawConversion<,>).Name);
      // POC: should we explode if no found?
      if (secondaryType != null)
      {
        containerBuilder.ContainerBuilder
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
        containerBuilder.ContainerBuilder
          .RegisterType(other.type)
          .Keyed<T>($"{other.name}|{other.rank}")
          .InstancePerLifetimeScope();

        // POC: not sure yet if...
        // * This should be an array of types
        // * Whether the scope should be modified or modifiable
        // * Whether this is in the write project... hmmm
        // POC: IsAssignableFrom()
        // NOT very DRY
        secondaryType = first.type.GetInterface(typeof(IRawConversion<,>).Name);
        // POC: should we explode if no found?
        if (secondaryType != null)
        {
          containerBuilder.ContainerBuilder.RegisterType(first.type).As(secondaryType).InstancePerLifetimeScope();
        }
      }
    }
  }
}

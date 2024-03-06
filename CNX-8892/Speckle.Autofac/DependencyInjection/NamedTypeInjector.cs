using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Speckle.Core.Logging;

namespace Speckle.Autofac.DependencyInjection;

public static class NamedTypeInjector
{
  public static ContainerBuilder InjectNamedTypes<T>(this ContainerBuilder containerBuilder)
    where T : class
  {
    List<Type> types = new();

    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.StartsWith("Speckle")))
    {
      try
      {
        var asmTypes = asm.GetTypes();
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

      // first type
      var first = namedTypes[0];

      containerBuilder.RegisterType(first.type).Keyed<T>(first.name).SingleInstance();

      // register subsequent types with rank
      namedTypes.RemoveAt(0);
      foreach (var other in namedTypes)
      {
        containerBuilder.RegisterType(other.type).Keyed<T>($"{other.name}|{other.rank}").SingleInstance();
      }
    }

    return containerBuilder;
  }
}

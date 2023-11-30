using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Speckle.Core.Kits;

namespace ConnectorGrasshopperUtils;

public static class CSOUtils
{
  public static List<Type> ListAvailableTypes(bool includeDeprecated = true)
  {
    // exclude types that don't have any constructors with a SchemaInfo attribute
    return KitManager.Types.Where(x => GetValidConstr(x, includeDeprecated).Any()).OrderBy(x => x.Name).ToList();
  }

  public static IEnumerable<ConstructorInfo> GetValidConstr(Type type, bool includeDeprecated = true)
  {
    return type.GetConstructors()
      .Where(y =>
      {
        var hasSchemaInfo = y.GetCustomAttribute<SchemaInfo>() != null;
        var isDeprecated = y.GetCustomAttribute<SchemaDeprecated>() != null;
        return includeDeprecated ? hasSchemaInfo : hasSchemaInfo && !isDeprecated;
      });
  }

  public static ConstructorInfo FindConstructor(string ConstructorName, string TypeName)
  {
    var type = KitManager.Types.FirstOrDefault(x => x.FullName == TypeName);
    if (type == null)
    {
      return null;
    }

    var constructors = GetValidConstr(type);
    var constructor = constructors.FirstOrDefault(x => MethodFullName(x) == ConstructorName);
    return constructor;
  }

  public static string MethodFullName(MethodBase m)
  {
    var s = m.ReflectedType.FullName;
    if (!m.IsConstructor)
    {
      s += ".";
    }

    s += m.Name;
    if (m.GetParameters().Any())
    {
      //jamie rainfall bug, had to replace + with .
      s +=
        "("
        + string.Join(
          ",",
          m.GetParameters().Select(o => string.Format("{0}", o.ParameterType).Replace("+", ".")).ToArray()
        )
        + ")";
    }

    return s;
  }
}

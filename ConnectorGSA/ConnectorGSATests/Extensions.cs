using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorGSATests
{
  public static class Extensions
  {
    public static bool IsList(this PropertyInfo pi, out Type listType)
    {
      if (pi.PropertyType.GetTypeInfo().IsGenericType)
      {
        var isList = pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
        listType = isList ? pi.PropertyType.GenericTypeArguments.First() : null;
        return isList;
      }
      listType = null;
      return false;
    }

    public static IEnumerable<Type> GetBaseClasses(this Type type)
    {
      return type.BaseType == typeof(object)
          ? type.GetInterfaces()
          : Enumerable
              .Repeat(type.BaseType, 1)
              .Concat(type.BaseType.GetBaseClasses())
              .Distinct();
    }

    public static void UpsertDictionary<T, U>(this Dictionary<T, List<U>> d, T key, IEnumerable<U> values)
    {
      if (!d.ContainsKey(key))
      {
        d.Add(key, values.ToList());
      }
      foreach (var v in values)
      {
        if (!d[key].Contains(v))
        {
          d[key].Add(v);
        }
      }
    }

    public static void UpsertDictionary<T, U>(this Dictionary<T, List<U>> d, T key, U value)
    {
      if (!d.ContainsKey(key))
      {
        d.Add(key, new List<U>());
      }
      if (!d[key].Contains(value))
      {
        d[key].Add(value);
      }
    }
  }
}

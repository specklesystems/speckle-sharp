using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Speckle.ConnectorGSA.Proxy
{
  public static class Extensions
  {
    /// <summary>
    /// Splits lists, keeping entities encapsulated by "" together.
    /// </summary>
    /// <param name="list">String to split</param>
    /// <param name="delimiter">Delimiter</param>
    /// <returns>Array of strings containing list entries</returns>
    public static string[] ListSplit(this string list, string delimiter)
    {
      return Regex.Split(list, delimiter + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

    public static string[] ListSplit(this string list, char delimiter)
    {
      return Regex.Split(list, delimiter + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

    public static double ToDouble(this object o)
    {
      try
      {
        var d = Convert.ToDouble(o);
        return d;
      }
      catch
      {
        return 0d;
      }
    }

    public static void UpsertList<T>(this IList<T> l, T value)
    {
      if (!l.Contains(value))
      {
        l.Add(value);
      }
    }

    public static void UpsertList<T>(this IList<T> l, IEnumerable<T> values)
    {
      foreach (var v in values)
      {
        if (!l.Contains(v))
        {
          l.Add(v);
        }
      }
    }

    public static void UpsertDictionary<T,U>(this Dictionary<T, HashSet<U>> d, T key, U value)
    {
      if (!d.ContainsKey(key))
      {
        d.Add(key, new HashSet<U>());
      }
      if (!d[key].Contains(value))
      {
        d[key].Add(value);
      }
    }

    public static bool ValidNonZero(this double? v)
    {
      return v.HasValue && v > 0;
    }

    public static bool ValidNonZero(this int? v)
    {
      return v.HasValue && v > 0;
    }

    public static string GridExpansionToString(this GridExpansion expansion)
    {
      switch (expansion)
      {
        case GridExpansion.PlaneAspect: return "PLANE_ASPECT";
        case GridExpansion.PlaneSmooth: return "PLANE_SMOOTH";
        case GridExpansion.PlaneCorner: return "PLANE_CORNER";
        default: return "LEGACY";
      }
    }

    public static GridExpansion StringToGridExpansion(string expansion)
    {
      switch (expansion)
      {
        case "PLANE_ASPECT": return GridExpansion.PlaneAspect;
        case "PLANE_SMOOTH": return GridExpansion.PlaneSmooth;
        case "PLANE_CORNER": return GridExpansion.PlaneCorner;
        default: return GridExpansion.Legacy;
      }
    }
  }
}

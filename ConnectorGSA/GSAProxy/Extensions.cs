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

    /// <summary>
    /// Will get the string value for a given enums value, this will
    /// only work if you assign the StringValue attribute to
    /// the items in your enum.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetStringValue(this IConvertible value)
    {
      // Get the type
      var type = value.GetType();

      // Get fieldinfo for this type
      var fieldInfo = type.GetField(value.ToString());

      // Get the stringvalue attributes
      var attribs = fieldInfo.GetCustomAttributes(
          typeof(StringValue), false) as StringValue[];

      // Return the first if there was a match.
      return attribs.Length > 0 ? attribs[0].Value : null;
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

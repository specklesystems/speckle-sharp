using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorGSA.Proxy
{
  public static class Extensions
  {
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
  }
}

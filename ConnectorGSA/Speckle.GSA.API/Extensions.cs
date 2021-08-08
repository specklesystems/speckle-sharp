using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Speckle.GSA.API
{
  public static class Extensions
  {
    /// <summary>
    /// Extract attribute from GSAObject objects or type.
    /// </summary>
    /// <param name="t">GSAObject objects or type</param>
    /// <param name="attribute">Attribute to extract</param>
    /// <returns>Attribute value</returns>
    public static object GetAttribute<T>(this object t, string attribute)
    {
      try
      {
        var attObj = (t is Type) ? Attribute.GetCustomAttribute((Type)t, typeof(T)) : Attribute.GetCustomAttribute(t.GetType(), typeof(T));
        return typeof(T).GetProperty(attribute).GetValue(attObj);
      }
      catch { return null; }
    }

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

    // <summary>
    /// Checks if the string contains only digits.
    /// </summary>
    /// <param name="str">String</param>
    /// <returns>True if string contails only digits</returns>
    public static bool IsDigits(this string str)
    {
      foreach (var c in str)
      {
        if (c < '0' || c > '9')
        {
          return false;
        }
      }
      return true;
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

    public static GridExpansion StringToGridExpansion(this string expansion)
    {
      switch (expansion)
      {
        case "PLANE_ASPECT": return GridExpansion.PlaneAspect;
        case "PLANE_SMOOTH": return GridExpansion.PlaneSmooth;
        case "PLANE_CORNER": return GridExpansion.PlaneCorner;
        default: return GridExpansion.Legacy;
      }
    }

    public static StructuralLoadCaseType StringToLoadCaseType(this string type)
    {
      switch (type)
      {
        case "DEAD":
        case "LC_PERM_SELF":
          return StructuralLoadCaseType.Dead;
        case "LC_VAR_IMP": return StructuralLoadCaseType.Live;
        case "WIND": return StructuralLoadCaseType.Wind;
        case "SNOW": return StructuralLoadCaseType.Snow;
        case "SEISMIC": return StructuralLoadCaseType.Earthquake;
        case "LC_PERM_SOIL": return StructuralLoadCaseType.Soil;
        case "LC_VAR_TEMP": return StructuralLoadCaseType.Thermal;
        default: return StructuralLoadCaseType.Generic;
      }
    }

    public static string LoadCaseTypeToString(this StructuralLoadCaseType caseType)
    {
      switch (caseType)
      {
        case StructuralLoadCaseType.Dead: return ("LC_PERM_SELF");
        case StructuralLoadCaseType.Live: return ("LC_VAR_IMP");
        case StructuralLoadCaseType.Wind: return ("WIND");
        case StructuralLoadCaseType.Snow: return ("SNOW");
        case StructuralLoadCaseType.Earthquake: return ("SEISMIC");
        case StructuralLoadCaseType.Soil: return ("LC_PERM_SOIL");
        case StructuralLoadCaseType.Thermal: return ("LC_VAR_TEMP");
        default: return ("LC_UNDEF");
      }
    }

    public static bool[] RestraintBoolArrayFromCode(this string code)
    {
      if (code == "free")
      {
        return new bool[] { false, false, false, false, false, false };
      }
      else if (code == "pin")
      {
        return new bool[] { true, true, true, false, false, false };
      }
      else if (code == "fix")
      {
        return new bool[] { true, true, true, true, true, true };
      }
      else
      {
        var fixities = new bool[6];

        var codeRemaining = code;
        int prevLength;
        do
        {
          prevLength = codeRemaining.Length;
          if (codeRemaining.Contains("xxx"))
          {
            fixities[0] = true;
            fixities[3] = true;
            codeRemaining = codeRemaining.Replace("xxx", "");
          }
          else if (codeRemaining.Contains("xx"))
          {
            fixities[3] = true;
            codeRemaining = codeRemaining.Replace("xx", "");
          }
          else if (codeRemaining.Contains("x"))
          {
            fixities[0] = true;
            codeRemaining = codeRemaining.Replace("x", "");
          }

          if (codeRemaining.Contains("yyy"))
          {
            fixities[1] = true;
            fixities[4] = true;
            codeRemaining = codeRemaining.Replace("yyy", "");
          }
          else if (codeRemaining.Contains("yy"))
          {
            fixities[4] = true;
            codeRemaining = codeRemaining.Replace("yy", "");
          }
          else if (codeRemaining.Contains("y"))
          {
            fixities[1] = true;
            codeRemaining = codeRemaining.Replace("y", "");
          }

          if (codeRemaining.Contains("zzz"))
          {
            fixities[2] = true;
            fixities[5] = true;
            codeRemaining = codeRemaining.Replace("zzz", "");
          }
          else if (codeRemaining.Contains("zz"))
          {
            fixities[5] = true;
            codeRemaining = codeRemaining.Replace("zz", "");
          }
          else if (codeRemaining.Contains("z"))
          {
            fixities[2] = true;
            codeRemaining = codeRemaining.Replace("z", "");
          }
        } while (codeRemaining.Length > 0 && (codeRemaining.Length < prevLength));

        return fixities;
      }
    }

    public static bool TryParseStringValue<T>(this string v, out T value) where T : IConvertible
    {
      if (!typeof(T).IsEnum)
      {
        throw new ArgumentException("T must be an enumerated type");
      }
      var enumValues = typeof(T).GetEnumValues().OfType<T>().ToDictionary(ev => GetStringValue(ev), ev => ev);
      if (enumValues.Keys.Any(k => k.Equals(v, StringComparison.InvariantCultureIgnoreCase)))
      {
        value = enumValues[v];
        return true;
      }
      value = default(T);
      return false;
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

    public static IEnumerable<Type> GetEnumerableOfType<T>() where T : class
    {
      return Assembly.GetAssembly(typeof(T)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)));
    }
  }
}

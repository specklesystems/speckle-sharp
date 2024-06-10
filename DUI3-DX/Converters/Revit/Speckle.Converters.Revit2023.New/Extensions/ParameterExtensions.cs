using Speckle.Converters.Common;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Extensions;

public static class ParameterExtensions
{
  /// <summary>
  /// Shared parameters use a GUID to be uniquely identified
  /// Other parameters use a BuiltInParameter enum
  /// </summary>
  /// <param name="rp"></param>
  /// <returns></returns>
  public static string GetInternalName(this IRevitParameter rp)
  {
    if (rp.IsShared)
    {
      return rp.GUID.ToString();
    }
    else
    {
      var def = rp.Definition.ToInternal().NotNull();
      if (def.BuiltInParameter == RevitBuiltInParameter.INVALID)
      {
        return def.Name;
      }

      return def.BuiltInParameter.ToString();
    }
  }

  public static RevitBuiltInParameter? GetBuiltInParameter(this IRevitParameter rp)
  {
    var def = rp.Definition.ToInternal();
    return def?.BuiltInParameter;
  }
}

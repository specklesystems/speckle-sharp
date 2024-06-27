using Autodesk.Revit.DB;

namespace Speckle.Converters.RevitShared.Extensions;

public static class ParameterExtensions
{
  /// <summary>
  /// Shared parameters use a GUID to be uniquely identified
  /// Other parameters use a BuiltInParameter enum
  /// </summary>
  /// <param name="rp"></param>
  /// <returns></returns>
  public static string GetInternalName(this DB.Parameter rp)
  {
    if (rp.IsShared)
    {
      return rp.GUID.ToString();
    }
    else
    {
      var def = (InternalDefinition)rp.Definition;
      if (def.BuiltInParameter == BuiltInParameter.INVALID)
      {
        return def.Name;
      }

      return def.BuiltInParameter.ToString();
    }
  }

  public static BuiltInParameter? GetBuiltInParameter(this Parameter rp)
  {
    var def = rp.Definition as InternalDefinition;
    return def?.BuiltInParameter;
  }
}

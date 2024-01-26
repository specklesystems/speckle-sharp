using System.Collections.Generic;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;

namespace Objects.Utils;

public static class Parameters
{
  /// <summary>
  /// Turns a List of Parameters into a Base so that it can be used with the Speckle parameters prop
  /// </summary>
  /// <param name="parameters"></param>
  /// <returns></returns>
  public static Base? ToBase(this List<Parameter> parameters)
  {
    if (parameters == null)
    {
      return null;
    }

    var @base = new Base();

    foreach (Parameter p in parameters)
    {
      //if an applicationId is defined (BuiltInName) use that as key, otherwise use the display name
      var key = string.IsNullOrEmpty(p.applicationInternalName) ? p.name : p.applicationInternalName;
      if (string.IsNullOrEmpty(key) || @base[key] != null)
      {
        continue;
      }

      @base[key] = p;
    }

    return @base;
  }
}

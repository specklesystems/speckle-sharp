using Speckle.Core.Logging;

namespace Speckle.Core.Models;

public static class DynamicBaseExtensions
{
  public static T Get<T>(this DynamicBase dynamicBase, string key)
  {
    var val = dynamicBase[key];
    if (val is null)
    {
      throw new SpeckleException($"'{key}' was not found.");
    }
    return (T)val;
  }
}

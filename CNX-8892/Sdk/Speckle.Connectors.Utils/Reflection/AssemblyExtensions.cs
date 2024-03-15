using System.Linq;
using System.Reflection;

namespace Speckle.Connectors.Utils.Reflection;

public static class AssemblyExtensions
{
  public static string GetVersion(this Assembly assembly)
  {
    // this is adapted from Serilog extension method, but we might find the fallback is enough: assembly.GetName()?.Version?.ToString();
    var attribute = assembly.GetCustomAttributes().OfType<AssemblyInformationalVersionAttribute>().FirstOrDefault();
    if (attribute != null)
    {
      return attribute.InformationalVersion;
    }

    // otherwise use assembly version
    // POC: missing version?
    return assembly.GetName()?.Version?.ToString() ?? "Missing Version";
  }
}

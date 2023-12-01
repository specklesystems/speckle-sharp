using Speckle.Core.Kits;

namespace ConnectorRhinoWebUI.Utils;

public static class Utils
{
#if RHINO6
  public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v6);
  public static string AppName = "Rhino";
#elif RHINO7
    public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v7);
    public static string AppName = "Rhino";
#else
  public static readonly string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v7);
  public static readonly string AppName = "Rhino";
#endif
}

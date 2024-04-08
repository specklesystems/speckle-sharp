using System.Collections.Generic;
using System.DoubleNumerics;
using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

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

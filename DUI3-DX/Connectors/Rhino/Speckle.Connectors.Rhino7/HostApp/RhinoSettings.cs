using System.Collections.Generic;
using System.IO;
using Speckle.Core.Kits;

namespace Speckle.Connectors.Rhino7.HostApp;

public class RhinoSettings
{
  public RhinoSettings(HostApplication hostAppInfo, HostAppVersion hostAppVersion)
  {
    HostAppInfo = hostAppInfo;
    HostAppVersion = hostAppVersion;
    Modules = new[] { new DirectoryInfo(typeof(RhinoSettings).Assembly.Location).Parent.FullName };
  }

  public HostApplication HostAppInfo { get; private set; }
  public HostAppVersion HostAppVersion { get; private set; }

  public IReadOnlyList<string> Modules { get; private set; }
}

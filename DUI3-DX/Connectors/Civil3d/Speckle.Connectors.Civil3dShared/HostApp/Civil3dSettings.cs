using System.IO;
using Speckle.Core.Kits; // POC: Must go https://spockle.atlassian.net/browse/CNX-9325

namespace Speckle.Connectors.Civil3d.HostApp;

public class Civil3dSettings
{
  public Civil3dSettings(HostApplication hostAppInfo, HostAppVersion hostAppVersion)
  {
    HostAppInfo = hostAppInfo;
    HostAppVersion = hostAppVersion;
    var dir = new DirectoryInfo(typeof(Civil3dSettings).Assembly.Location);
    Modules = new[] { dir.Parent.FullName };
  }

  public HostApplication HostAppInfo { get; private set; }
  public HostAppVersion HostAppVersion { get; private set; }

  public IReadOnlyList<string> Modules { get; private set; }
}

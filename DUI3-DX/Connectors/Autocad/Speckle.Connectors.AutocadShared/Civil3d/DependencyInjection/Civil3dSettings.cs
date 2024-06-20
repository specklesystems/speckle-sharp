using System.IO;
using Speckle.Connectors.Autocad.Interfaces;
using Speckle.Core.Kits; // POC: Must go https://spockle.atlassian.net/browse/CNX-9325

namespace Speckle.Connectors.Civil3d.DependencyInjection;

public class Civil3dSettings : IAutocadSettings
{
  public Civil3dSettings(HostApplication hostAppInfo, HostAppVersion hostAppVersion)
  {
    HostAppInfo = hostAppInfo;
    HostAppVersion = hostAppVersion;
    var dir = new DirectoryInfo(typeof(Civil3dSettings).Assembly.Location);
    Modules = new[] { dir.Parent.FullName };
  }

  public HostApplication HostAppInfo { get; set; }
  public HostAppVersion HostAppVersion { get; set; }

  public IReadOnlyList<string> Modules { get; set; }
}

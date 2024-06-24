using System.IO;
using Speckle.Core.Kits; // POC: Must go https://spockle.atlassian.net/browse/CNX-9325

namespace Speckle.Connectors.Autocad.DependencyInjection;

public class AutocadSettings
{
  public AutocadSettings(HostApplication hostAppInfo, HostAppVersion hostAppVersion)
  {
    HostAppInfo = hostAppInfo;
    HostAppVersion = hostAppVersion;
    var dir = new DirectoryInfo(typeof(AutocadSettings).Assembly.Location);
    Modules = new[] { dir.Parent.FullName };
  }

  public HostApplication HostAppInfo { get; set; }
  public HostAppVersion HostAppVersion { get; set; }

  public IReadOnlyList<string> Modules { get; set; }
}

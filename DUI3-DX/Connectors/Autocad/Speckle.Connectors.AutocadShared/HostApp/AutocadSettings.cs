using System.IO;
using Speckle.Core.Kits;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadSettings
{
  public AutocadSettings(HostApplication hostAppInfo, HostAppVersion hostAppVersion)
  {
    HostAppInfo = hostAppInfo;
    HostAppVersion = hostAppVersion;
    Modules = new[] { new DirectoryInfo(typeof(AutocadSettings).Assembly.Location).Parent.FullName };
  }

  public HostApplication HostAppInfo { get; private set; }
  public HostAppVersion HostAppVersion { get; private set; }

  public IReadOnlyList<string> Modules { get; private set; }
}

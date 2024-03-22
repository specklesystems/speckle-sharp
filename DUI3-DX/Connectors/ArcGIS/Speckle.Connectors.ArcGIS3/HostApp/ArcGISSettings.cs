using System.IO;
using Speckle.Core.Kits;

namespace Speckle.Connectors.ArcGIS.HostApp;

public class ArcGISSettings
{
  public ArcGISSettings(HostApplication hostAppInfo, HostAppVersion hostAppVersion)
  {
    HostAppInfo = hostAppInfo;
    HostAppVersion = hostAppVersion;
    Modules = new[] { new DirectoryInfo(typeof(ArcGISSettings).Assembly.ToString()).Parent!.FullName };
  }

  public HostApplication HostAppInfo { get; private set; }
  public HostAppVersion HostAppVersion { get; private set; }

  public IReadOnlyList<string> Modules { get; private set; }
}

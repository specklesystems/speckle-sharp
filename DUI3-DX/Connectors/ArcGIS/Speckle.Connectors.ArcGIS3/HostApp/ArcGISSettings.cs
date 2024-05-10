using System.IO;
using Speckle.Core.Kits;

namespace Speckle.Connectors.ArcGIS.HostApp;

//poc: dupe code bewtween connectors
public class ArcGISSettings
{
  public ArcGISSettings(HostApplication hostAppInfo, HostAppVersion hostAppVersion)
  {
    HostAppInfo = hostAppInfo;
    HostAppVersion = hostAppVersion;
    Modules = new[] { new DirectoryInfo(typeof(ArcGISSettings).Assembly.Location).Parent!.FullName }; //poc: Net6 requires us to use this `location` property rather than ToString, should we use this everywhere?
  }

  public HostApplication HostAppInfo { get; private set; }
  public HostAppVersion HostAppVersion { get; private set; }

  public IReadOnlyList<string> Modules { get; private set; }
}

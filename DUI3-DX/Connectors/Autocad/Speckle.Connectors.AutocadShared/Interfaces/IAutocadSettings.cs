using Speckle.Core.Kits;

namespace Speckle.Connectors.Autocad.Interfaces;

public interface IAutocadSettings
{
  public HostApplication HostAppInfo { get; set; }
  public HostAppVersion HostAppVersion { get; set; }

  public IReadOnlyList<string> Modules { get; set; }
}

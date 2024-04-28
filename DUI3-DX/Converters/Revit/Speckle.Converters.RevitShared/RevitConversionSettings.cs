namespace Speckle.Converters.RevitShared;

// POC: probably NOT the right place, probably needs passing in with the send/rcv operation
// not clear how this should get configured or if we should have it, the shape probably needs to change
// this was dragged in because it (or something like it) is required for reference point conversion.
// have made it a strongly typed thing encapsulating a dictionary so as not to rely on injecting a weak type.
public class RevitConversionSettings
{
  private Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

  public bool TryGetSettingString(string key, out string value) => Settings.TryGetValue(key, out value);

  public string this[string key]
  {
    get => Settings[key];
    set => Settings[key] = value;
  }
}

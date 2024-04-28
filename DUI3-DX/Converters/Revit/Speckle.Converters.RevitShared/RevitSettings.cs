namespace Speckle.Converters.RevitShared;

// POC: probably NOT the right place, probably needs passing in with the send/rcv operation
public class RevitSettings
{
  private Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

  public bool TryGetSettingString(string key, out string value) => Settings.TryGetValue(key, out value);

  public string this[string key]
  {
    get => Settings[key];
    set => Settings[key] = value;
  }
}

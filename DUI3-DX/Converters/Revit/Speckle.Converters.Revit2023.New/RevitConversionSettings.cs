using Speckle.InterfaceGenerator;

namespace Speckle.Converters.Revit2023;

[GenerateAutoInterface]
public class RevitConversionSettings : IRevitConversionSettings
{
  private Dictionary<string, string> Settings { get; } = new();

  public bool TryGetSettingString(string key, out string value) => Settings.TryGetValue(key, out value);

  public string this[string key]
  {
    get => Settings[key];
    set => Settings[key] = value;
  }
}

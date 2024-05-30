using Speckle.Connectors.DUI.Utils;

namespace Speckle.Connectors.DUI.Settings;

public class CardSetting : DiscriminatedObject
{
  public string? Id { get; set; }
  public string? Title { get; set; }
  public string? Type { get; set; }
  public object? Value { get; set; }
  public List<string>? Enum { get; set; }
}

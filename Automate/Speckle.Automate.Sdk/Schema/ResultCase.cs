namespace Speckle.Automate.Sdk.Schema;

public struct ResultCase
{
  public string Category { get; set; }
  public string Level { get; set; }
  public List<string> ObjectIds { get; set; }
  public string? Message { get; set; }
  public Dictionary<string, object>? Metadata { get; set; }
  public Dictionary<string, object>? VisualOverrides { get; set; }
}

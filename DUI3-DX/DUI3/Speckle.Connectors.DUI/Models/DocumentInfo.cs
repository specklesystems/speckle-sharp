namespace Speckle.Connectors.DUI.Models;

public record DocumentInfo(string Location, string Name, string Id)
{
  //?.Replace("\\", "\\\\"); // for some reason, when returning variables from a direct binding call
  //we don't need this. nevertheless, after switching to a post response back to the ui,
  //we need this to ensure deserialization in js doesn't throw. it's frustrating!
  public string? Message { get; set; }
}

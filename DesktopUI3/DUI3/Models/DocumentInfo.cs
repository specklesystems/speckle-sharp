using JetBrains.Annotations;

namespace DUI3.Models;

[PublicAPI]
public class DocumentInfo
{
  private string _location;

  public string Location
  {
    get => _location;
    set => _location = value?.Replace("\\", "\\\\"); // for some reason, when returning variables from a direct binding call we don't need this. nevertheless, after switching to a post response back to the ui, we need this to ensure deserialization in js doesn't throw. it's frustrating!
  }

  public string Name { get; set; }
  public string Id { get; set; }
}

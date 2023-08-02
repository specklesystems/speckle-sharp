using JetBrains.Annotations;

namespace DUI3.Models;

[PublicAPI]
public class DocumentInfo
{
  public string Location { get; set; }
  public string Name { get; set; }
  public string Id { get; set; }
}

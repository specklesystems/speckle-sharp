#nullable enable
using System.Collections.Generic;
using DUI3.Utils;

namespace DUI3.Settings;

public class CardSetting : DiscriminatedObject
{
  public string Id { get; set; }
  public string Title { get; set; }
  public string Type { get; set; }
  public object Default { get; set; }
  public List<string>? Enum { get; set; }
}

using DUI3.Utils;

namespace DUI3.Models;

public abstract class SendSettings : DiscriminatedObject
{
  public bool BasicSettingValue { get; set; } = true;
}

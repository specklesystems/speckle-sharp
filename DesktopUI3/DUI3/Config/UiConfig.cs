using System.Collections.Generic;
using DUI3.Utils;

namespace DUI3.Config;

public class UiConfig : DiscriminatedObject
{
  public GlobalConfig Global { get; set; }
  
  public Dictionary<string, ConnectorConfig> Connectors { get; set; }
}

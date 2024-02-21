using System.Collections.Generic;
using Speckle.Connectors.DUI.Utils;

namespace Speckle.Connectors.DUI.Config;

public class UiConfig : DiscriminatedObject
{
  public GlobalConfig Global { get; set; }

  public Dictionary<string, ConnectorConfig> Connectors { get; set; }
}

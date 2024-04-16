using System;
using System.Runtime.Serialization;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.DUI.Bindings;

/// <summary>
/// POC: Simple config binding, as it was driving Dim nuts he couldn't swap to a dark theme.
/// How does it store configs? In a sqlite db called 'DUI3Config', we create a row for each host application:
/// [ hash,     contents         ]
/// ['Rhino',   serialised config]
/// ['Revit',   serialised config]
/// </summary>
public class ConfigBinding : IBinding
{
  public string Name => "configBinding";
  public IBridge Parent { get; }
  private SQLiteTransport ConfigStorage { get; }
  private readonly string _connectorName;
  private readonly JsonSerializerSettings _serializerOptions;

  public ConfigBinding(IBridge bridge, JsonSerializerSettings serializerOptions, string connectorName)
  {
    Parent = bridge;
    ConfigStorage = new SQLiteTransport(scope: "DUI3Config");
    _connectorName = connectorName;
    _serializerOptions = serializerOptions;
  }

  public ConnectorConfig GetConfig()
  {
    var rawConfig = ConfigStorage.GetObject(_connectorName);
    if (rawConfig is null)
    {
      return SeedConfig();
    }

    try
    {
      var config = JsonConvert.DeserializeObject<ConnectorConfig>(rawConfig, _serializerOptions);
      if (config is null)
      {
        throw new SerializationException("Failed to deserialize config");
      }

      return config;
    }
    catch (SerializationException)
    {
      return SeedConfig();
    }
  }

  private ConnectorConfig SeedConfig()
  {
    var cfg = new ConnectorConfig();
    UpdateConfig(cfg);
    return cfg;
  }

  public void UpdateConfig(ConnectorConfig config)
  {
    var str = JsonConvert.SerializeObject(config, _serializerOptions);
    ConfigStorage.UpdateObject(_connectorName, str);
  }
}

/// <summary>
/// POC: A simple POCO for keeping track of settings. I see this as extensible in the future by each host application if and when we will need global per-app connector settings.
/// </summary>
public class ConnectorConfig
{
  public bool DarkTheme { get; set; } = true;
}

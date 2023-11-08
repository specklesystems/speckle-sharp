using System;
using System.Collections.Generic;
using DUI3.Utils;
using JetBrains.Annotations;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace DUI3.Bindings;

public class ConfigBinding : IBinding
{
  public string Name { get; set; } = "configBinding";
  public IBridge Parent { get; set; }
  private string HostAppName { get; }
  private static readonly SQLiteTransport ConfigStorage = new(scope: "Config");
  private readonly JsonSerializerSettings _serializerOptions = SerializationSettingsFactory.GetSerializerSettings();

  public ConfigBinding(string hostAppName)
  {
    this.HostAppName = hostAppName;
  }
  
  [PublicAPI]
  public UiConfig GetConfig()
  {
    try
    {
      return GetOrInitConfig();
    }
    catch (Exception e)
    {
      UiConfig uiConfig = InitDefaultConfig();
      ConfigStorage.UpdateObject("configDUI3", JsonConvert.SerializeObject(uiConfig, _serializerOptions));
      return uiConfig;
    }
  }
  
  public void UpdateGlobalConfig(GlobalConfig newGlobalConfig)
  {
    try
    {
      UiConfig uiConfig = GetOrInitConfig();
      uiConfig.Global = newGlobalConfig;
      ConfigStorage.UpdateObject("configDUI3", JsonConvert.SerializeObject(uiConfig, _serializerOptions));
    }
    catch (Exception e)
    {
      // TODO: Log error
    }
  }
  
  public void UpdateConnectorConfig(ConnectorConfig newConnectorConfig)
  {
    try
    {
      UiConfig uiConfig = GetOrInitConfig();
      uiConfig.Connectors[HostAppName] = newConnectorConfig;
      ConfigStorage.UpdateObject("configDUI3", JsonConvert.SerializeObject(uiConfig, _serializerOptions));
    }
    catch (Exception e)
    {
      // TODO: Log error
    }
  }
  
  private UiConfig GetOrInitConfig()
  {
    string configDui3String = ConfigStorage.GetObject("configDUI3");

    if (string.IsNullOrEmpty(configDui3String))
    {
      return InitDefaultConfig();
    }
    
    UiConfig config = JsonConvert.DeserializeObject<UiConfig>(configDui3String, _serializerOptions);

    if (config.Connectors.ContainsKey(HostAppName.ToLower())) return config;
    
    ConnectorConfig connectorConfig = new (HostAppName);
    config.Connectors.Add(HostAppName, connectorConfig);
    ConfigStorage.UpdateObject("configDUI3", JsonConvert.SerializeObject(config, _serializerOptions));
    return config;
  }

  private UiConfig InitDefaultConfig()
  {
    Dictionary<string, ConnectorConfig> defaultConfigs = new() { { HostAppName, new ConnectorConfig(HostAppName) } };
    UiConfig defaultConfig = new() { Global = new GlobalConfig(), Connectors = defaultConfigs };
    string serializedConfigs = JsonConvert.SerializeObject(defaultConfig, _serializerOptions);
    ConfigStorage.UpdateObject("configDUI3", serializedConfigs);
    return defaultConfig;
  }
}

public class GlobalConfig : DiscriminatedObject
{
  public bool OnboardingCompleted { get; set; }
}

public class ConnectorConfig : DiscriminatedObject
{
  public string HostApp { set; get; }
  
  public bool DarkTheme { set; get; }

  public ConnectorConfig() { }

  public ConnectorConfig(string hostApp)
  {
    HostApp = hostApp;
  }
}

public class UiConfig : DiscriminatedObject
{
  public GlobalConfig Global { get; set; }
  
  public Dictionary<string, ConnectorConfig> Connectors { get; set; }
}

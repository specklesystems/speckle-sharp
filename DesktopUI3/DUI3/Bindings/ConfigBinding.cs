using System;
using System.Collections.Generic;
using DUI3.Config;
using DUI3.Onboarding;
using DUI3.Utils;
using JetBrains.Annotations;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace DUI3.Bindings;

/// <summary>
/// Responsible to initialize, validate and retrieve configuration data from AppData/Config.db
/// </summary>
public class ConfigBinding : IBinding
{
  public string Name { get; set; } = "configBinding";
  public IBridge Parent { get; set; }
  private string HostAppName { get; }
  private Dictionary<string, OnboardingData> ConnectorOnboardings { get; }
  
  private static readonly SQLiteTransport ConfigStorage = new(scope: "Config");
  private readonly JsonSerializerSettings _serializerOptions = SerializationSettingsFactory.GetSerializerSettings();

  public ConfigBinding(string hostAppName, Dictionary<string, OnboardingData> connectorOnboardings = null)
  {
    this.HostAppName = hostAppName;
    
    // If connectorOnboardings is null, initialize it as an empty dictionary
    connectorOnboardings ??= new Dictionary<string, OnboardingData>();
    this.ConnectorOnboardings = connectorOnboardings;
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
      // Fallbacks to default configs if something wrong
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
    // 1 - If it is not exist, init and return.
    string configDui3String = ConfigStorage.GetObject("configDUI3");
    if (string.IsNullOrEmpty(configDui3String))
    {
      return InitDefaultConfig();
    }
    
    // 2- If connector config already exist in UiConfig, just return it.
    UiConfig config = JsonConvert.DeserializeObject<UiConfig>(configDui3String, _serializerOptions);
    if (config.Connectors.ContainsKey(HostAppName.ToLower()))
    {
      ConnectorConfig existingConnectorConfig = config.Connectors[HostAppName.ToLower()];
      bool updated = existingConnectorConfig.InitializeNewProperties();
      bool removedProperties = existingConnectorConfig.CheckRemovedProperties();
      if (updated || removedProperties)
      {
        ConfigStorage.UpdateObject("configDUI3", JsonConvert.SerializeObject(config, _serializerOptions));
      }
      
      return config;
    }
    
    // 3- If connector config didn't initialized yet, init and attach it, then return.
    ConnectorConfig connectorConfig = new (HostAppName, ConnectorOnboardings);
    config.Connectors.Add(HostAppName, connectorConfig);
    ConfigStorage.UpdateObject("configDUI3", JsonConvert.SerializeObject(config, _serializerOptions));
    return config;
  }

  private UiConfig InitDefaultConfig()
  {
    ConnectorConfig connectorConfig = new (HostAppName, ConnectorOnboardings);
    Dictionary<string, ConnectorConfig> defaultConfigs = new() { { HostAppName, connectorConfig } };
    UiConfig defaultConfig = new() { Global = new GlobalConfig(), Connectors = defaultConfigs };
    string serializedConfigs = JsonConvert.SerializeObject(defaultConfig, _serializerOptions);
    ConfigStorage.UpdateObject("configDUI3", serializedConfigs);
    return defaultConfig;
  }
}

using System.Collections.Generic;
using System.Diagnostics;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Config;
using Speckle.Connectors.DUI.Onboarding;
using Speckle.Connectors.DUI.Utils;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace Speckle.Connectors.DUI.Bindings;

/// <summary>
/// Responsible to initialize, validate and retrieve configuration data from AppData/Config.db
/// </summary>
public class ConfigBinding : IBinding
{
  private const string SQL_CONFIG_HASH = "configDUI3";

  public string Name { get; set; } = "configBinding";
  public IBridge Parent { get; set; }
  private string HostAppName { get; }
  private Dictionary<string, OnboardingData> ConnectorOnboardings { get; }

  private readonly SQLiteTransport _sqLiteTransport;
  private readonly JsonSerializerSettings _serializerOptions;

  public ConfigBinding(
    SQLiteTransport sqLiteTransport,
    JsonSerializerSettings serializerOptions,
    Dictionary<string, OnboardingData> connectorOnboardings = null
  )
  {
    _serializerOptions = serializerOptions;
    _sqLiteTransport = sqLiteTransport;

    // POC: from config
    this.HostAppName = "<Host App name>";

    // If connectorOnboardings is null, initialize it as an empty dictionary
    connectorOnboardings ??= new Dictionary<string, OnboardingData>();
    this.ConnectorOnboardings = connectorOnboardings;
  }

  public UiConfig GetConfig()
  {
    try
    {
      return GetOrInitConfig();
    }
    catch (SpeckleException e)
    {
      Debug.WriteLine(e.Message);
      // Fallbacks to default configs if something wrong
      UiConfig uiConfig = InitDefaultConfig();

      // POC: hardcoded string configDUI3
      _sqLiteTransport.UpdateObject(SQL_CONFIG_HASH, JsonConvert.SerializeObject(uiConfig, _serializerOptions));
      return uiConfig;
    }
  }

  public void UpdateGlobalConfig(GlobalConfig newGlobalConfig)
  {
    try
    {
      UiConfig uiConfig = GetOrInitConfig();
      uiConfig.Global = newGlobalConfig;

      // POC: hardcoded string configDUI3
      _sqLiteTransport.UpdateObject(SQL_CONFIG_HASH, JsonConvert.SerializeObject(uiConfig, _serializerOptions));
    }
    catch (SpeckleException e)
    {
      // POC: Log error, exception specificity, how should caller respond?
      Debug.WriteLine(e.Message);
    }
  }

  public void UpdateConnectorConfig(ConnectorConfig newConnectorConfig)
  {
    try
    {
      UiConfig uiConfig = GetOrInitConfig();
      uiConfig.Connectors[HostAppName] = newConnectorConfig;
      _sqLiteTransport.UpdateObject(SQL_CONFIG_HASH, JsonConvert.SerializeObject(uiConfig, _serializerOptions));
    }
    catch (SpeckleException e)
    {
      // POC: Log error, exception specificity, how should caller respond?
      Debug.WriteLine(e.Message);
    }
  }

  private void ValidateConfigs(UiConfig uiConfig, PropertyValidator config)
  {
    bool globalConfigsAdded = config.InitializeNewProperties();
    bool globalConfigRemoved = config.CheckRemovedProperties();
    if (globalConfigsAdded || globalConfigRemoved)
    {
      _sqLiteTransport.UpdateObject(SQL_CONFIG_HASH, JsonConvert.SerializeObject(uiConfig, _serializerOptions));
    }
  }

  private UiConfig GetOrInitConfig()
  {
    // 1 - If it is not exist, init and return.
    string configDui3String = _sqLiteTransport.GetObject(SQL_CONFIG_HASH);
    if (string.IsNullOrEmpty(configDui3String))
    {
      return InitDefaultConfig();
    }

    UiConfig config = JsonConvert.DeserializeObject<UiConfig>(configDui3String, _serializerOptions);

    // 2- Check global configs updated or not.
    ValidateConfigs(config, config.Global);

    // 3- If connector config already exist in UiConfig, just return it.
    if (config.Connectors.TryGetValue(HostAppName.ToLower(), out ConnectorConfig value))
    {
      ConnectorConfig existingConnectorConfig = value;
      ValidateConfigs(config, existingConnectorConfig);

      return config;
    }

    // 4- If connector config didn't initialized yet, init and attach it, then return.
    ConnectorConfig _connectorConfig = new(HostAppName, ConnectorOnboardings);
    config.Connectors.Add(HostAppName, _connectorConfig);
    _sqLiteTransport.UpdateObject(SQL_CONFIG_HASH, JsonConvert.SerializeObject(config, _serializerOptions));

    return config;
  }

  private UiConfig InitDefaultConfig()
  {
    ConnectorConfig connectorConfig = new(HostAppName, ConnectorOnboardings);
    Dictionary<string, ConnectorConfig> defaultConfigs = new() { { HostAppName, connectorConfig } };
    UiConfig defaultConfig = new() { Global = new GlobalConfig(), Connectors = defaultConfigs };
    string serializedConfigs = JsonConvert.SerializeObject(defaultConfig, _serializerOptions);
    _sqLiteTransport.UpdateObject(SQL_CONFIG_HASH, serializedConfigs);
    return defaultConfig;
  }
}

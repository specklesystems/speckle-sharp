using System;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace DesktopUI2.Models;

/// <summary>
/// Helps to save and load DUI configuration
/// </summary>
public static class ConfigManager
{
  private static SQLiteTransport ConfigStorage = new(scope: "Config");

  public static void Save(Config config)
  {
    ConfigStorage.UpdateObject("configDUI", JsonConvert.SerializeObject(config));
  }

  public static Config Load()
  {
    try
    {
      //dui and manager were sharing the same config!
      //splitting them to avoid overwriting settings
      var oldConfig = ConfigStorage.GetObject("config");
      var newConfig = ConfigStorage.GetObject("configDUI");

      Config deserializedConfig = !string.IsNullOrWhiteSpace(newConfig)
        ? JsonConvert.DeserializeObject<Config>(newConfig)
        : JsonConvert.DeserializeObject<Config>(oldConfig);

      return deserializedConfig;
    }
    catch (Exception e) { }
    return new Config();
  }
}

/// <summary>
/// DUI configuration
/// </summary>
public class Config
{
  public bool DarkTheme { set; get; }
  public bool OneClickMode { set; get; } = true;
  public bool ShowImportExportAlert { set; get; } = true;
  public bool UseFe2 { set; get; }
}

using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace DesktopUI2.Models
{
  /// <summary>
  /// Helps to save and load DUI configuration
  /// </summary>
  public static class ConfigManager
  {

    private static SQLiteTransport ConfigStorage = new SQLiteTransport(scope: "Config");

    public static void Save(Config config)
    {
      ConfigStorage.UpdateObject("config", JsonConvert.SerializeObject(config));
    }

    public static Config Load()
    {
      try
      {
        return JsonConvert.DeserializeObject<Config>(ConfigStorage.GetObject("config"));
      }
      catch { }
      return new Config();
    }
  }

  /// <summary>
  /// DUI configuration
  /// </summary>
  public class Config
  {
    public bool DarkTheme { set; get; }
    public bool ShowImportExportAlert { set; get; } = true;
  }


}

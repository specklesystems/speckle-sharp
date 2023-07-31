using System;
using System.Collections.Generic;
using System.Text.Json;
using Speckle.Core.Transports;

namespace DUI3.Bindings;

public class ConfigBinding : IBinding
{
  public string Name { get; set; } = "configBinding";
  public IBridge Parent { get; set; }

  private static readonly SQLiteTransport ConfigStorage = new(scope: "Config");
  
  public Config GetConfig()
  {
    try
    {
      var config = ConfigStorage.GetObject("configDUI3");
      if (string.IsNullOrEmpty(config)) return new Config();
      return JsonSerializer.Deserialize<Config>(config);
    }
    catch (Exception e)
    {
      // TODO: Log error
      return new Config();
    }
  }

  public void UpdateConfig(Config config)
  {
    try
    {
      ConfigStorage.UpdateObject("configDUI3", JsonSerializer.Serialize(config));
    }
    catch (Exception e)
    {
      // TODO: Log error
    }
  }
}

public class Config
{
  public bool DarkTheme { set; get; }
  /**
   * Meant to keep track of whether the v0 onboarding has been completed or not, separated by host app. E.g.:
   * { "Rhino" : true, "Revit": false }
   */
  public Dictionary<string, bool> OnboardingV0 { get; set; } = new Dictionary<string, bool>();
}

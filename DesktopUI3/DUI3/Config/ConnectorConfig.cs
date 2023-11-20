using System.Collections.Generic;
using DUI3.Onboarding;
using DUI3.Utils;

namespace DUI3.Config;

public class ConnectorConfig : PropertyValidator
{
  public string HostApp { set; get; }

  public bool? DarkTheme { set; get; } = false;
  
  public Dictionary<string, OnboardingData> Onboardings { get; set; }

  public ConnectorConfig() { }

  public ConnectorConfig(string hostApp, Dictionary<string, OnboardingData> onboardings)
  {
    HostApp = hostApp;
    this.Onboardings = onboardings;
  }
}

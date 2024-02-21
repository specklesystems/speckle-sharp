using System.Collections.Generic;
using Speckle.Connectors.DUI.Onboarding;
using Speckle.Connectors.DUI.Utils;

namespace Speckle.Connectors.DUI.Config;

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

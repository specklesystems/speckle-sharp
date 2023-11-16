using System.Collections.Generic;
using DUI3.Onboarding;
using DUI3.Utils;

namespace DUI3.Config;

public class GlobalConfig : DiscriminatedObject
{
  public string Test { get; set; } = "defaultValue";
  public bool OnboardingSkipped { get; set; }
  public Dictionary<string, OnboardingData> Onboardings { get; set; } = Factory.CreateDefaults();
}

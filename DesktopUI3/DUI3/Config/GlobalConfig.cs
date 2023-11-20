using System.Collections.Generic;
using DUI3.Onboarding;
using DUI3.Utils;

namespace DUI3.Config;

public class GlobalConfig : PropertyValidator
{
  public bool? OnboardingSkipped { get; set; } = false;
  public Dictionary<string, OnboardingData> Onboardings { get; set; } = Factory.CreateDefaults();
}

using System.Collections.Generic;
using Speckle.Connectors.DUI.Onboarding;
using Speckle.Connectors.DUI.Utils;

namespace Speckle.Connectors.DUI.Config;

public class GlobalConfig : PropertyValidator
{
  public bool? OnboardingSkipped { get; set; } = false;
  public Dictionary<string, OnboardingData> Onboardings { get; set; } = Factory.CreateDefaults();
}

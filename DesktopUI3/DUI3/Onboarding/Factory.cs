using System.Collections.Generic;

namespace DUI3.Onboarding;

public static class Factory
{
  public static OnboardingData CreateSend()
  {
    return new()
    {
      Title = "Send",
      Blurb = "Send first model to Speckleverse!",
      Completed = false,
      Page = "/onboarding/send",
    };
  }

  public static OnboardingData CreateReceive()
  {
    return new()
    {
      Title = "Receive",
      Blurb = "Receive first model from Speckleverse!",
      Completed = false,
      Page = "/onboarding/receive",
    };
  }

  public static Dictionary<string, OnboardingData> CreateDefaults()
  {
    return new Dictionary<string, OnboardingData>()
    {
      { "send", CreateSend() },
      { "receive", CreateReceive() }
    };
  }
}

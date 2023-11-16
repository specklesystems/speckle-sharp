using DUI3.Utils;

namespace DUI3.Onboarding;

public class OnboardingData : DiscriminatedObject
{
  public string Title { get; set; }
  public string Blurb { get; set; }
  public bool Completed { get; set; }
  public string Page { get; set; }
}

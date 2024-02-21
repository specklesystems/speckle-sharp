using Speckle.Connectors.DUI.Utils;

namespace Speckle.Connectors.DUI.Onboarding;

public class OnboardingData : DiscriminatedObject
{
  public string Title { get; set; }
  public string Blurb { get; set; }
  public bool? Completed { get; set; } = false;
  public string Page { get; set; }
}

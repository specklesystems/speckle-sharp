using Speckle.Core.Models;

namespace Objects.BuiltElements.AdvanceSteel;

public class AsteelSectionProfile : Base
{
  public string ProfSectionType { get; set; }

  public string ProfSectionName { get; set; }

  public AsteelSectionProfileDB SectionProfileDB { get; set; }
}

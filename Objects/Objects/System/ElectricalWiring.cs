using Objects.Definitions;
using Objects.Organization;

namespace Objects.System
{
  public class ElectricalWiring : CurveBasedElement
  {
  public string wiringType { get; set; }
  public Level level { get; set; }

    // to implement source app parameters interface from claire
  }
}

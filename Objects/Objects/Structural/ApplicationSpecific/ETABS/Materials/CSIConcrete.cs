using Speckle.Core.Kits;
using Objects.Structural.Materials;

namespace Objects.Structural.CSI.Materials
{

  public class CSIConcrete : Concrete
  {
    public int SSHysType { get; set; }
    public int SSType { get; set; }

    public double finalSlope { get; set; }

    public double frictionAngle { get; set; }
    public double dialationalAngle { get; set; }
    public CSIConcrete()
    {
    }
  }
}

using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.Properties
{
  public class PropertyDamper : Property
  {
    public PropertyTypeDamper damperType { get; set; }
    public double dampingX { get; set; }
    public double dampingY { get; set; }
    public double dampingZ { get; set; }
    public double dampingXX { get; set; }
    public double dampingYY { get; set; }
    public double dampingZZ { get; set; }

    public PropertyDamper() { }

    [SchemaInfo("PropertyDamper", "Creates a Speckle structural damper property", "Structural", "Properties")]
    public PropertyDamper(string name)
    {
      this.name = name;
    }

    [SchemaInfo("PropertyDamper (general)", "Creates a Speckle structural damper property (for 6 degrees of freedom)", "Structural", "Properties")]
    public PropertyDamper(string name, PropertyTypeDamper damperType, double dampingX = 0, double dampingY = 0, double dampingZ = 0, double dampingXX = 0, double dampingYY = 0, double dampingZZ = 0)
    {
      this.name = name;
      this.damperType = damperType;
      this.dampingX = dampingX;
      this.dampingY = dampingY;
      this.dampingZ = dampingZ;
      this.dampingXX = dampingXX;
      this.dampingYY = dampingYY;
      this.dampingZZ = dampingZZ;
    }
  }
}

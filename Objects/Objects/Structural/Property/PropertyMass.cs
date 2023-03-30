using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.Properties
{
  public class PropertyMass : Property // nodal constraint axis of the node assumed to be mass property axis
  {
    public double mass { get; set; }
    public double inertiaXX { get; set; }
    public double inertiaYY { get; set; }
    public double inertiaZZ { get; set; }
    public double inertiaXY { get; set; }
    public double inertiaYZ { get; set; }
    public double inertiaZX { get; set; }
    public bool massModified { get; set; } = false;
    public double massModifierX { get; set; }
    public double massModifierY { get; set; }
    public double massModifierZ { get; set; }
    public PropertyMass() { }

    [SchemaInfo("PropertyMass", "Creates a Speckle structural mass property", "Structural", "Properties")]
    public PropertyMass(string name)
    {
      this.name = name;
    }

    [SchemaInfo("PropertyMass (general)", "Creates a Speckle structural mass property", "Structural", "Properties")]
    public PropertyMass(string name, double mass, double inertiaXX = 0, double inertiaYY = 0, double inertiaZZ = 0, double inertiaXY = 0, double inertiaYZ = 0, double inertiaZX = 0, bool massModified = false, double massModifierX = 0, double massModifierY = 0, double massModifierZ = 0)
    {
      this.name = name;
      this.mass = mass;
      this.inertiaXX = inertiaXX;
      this.inertiaYY = inertiaYY;
      this.inertiaZZ = inertiaZZ;
      this.inertiaXY = inertiaXY;
      this.inertiaYZ = inertiaYZ;
      this.inertiaZX = inertiaZX;
      this.massModified = massModified;
      this.massModifierX = massModifierX;
      this.massModifierY = massModifierY;
      this.massModifierZ = massModifierZ;
    }
  }
}

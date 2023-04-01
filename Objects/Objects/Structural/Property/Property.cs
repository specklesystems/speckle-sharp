using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.Properties
{
  public class Property : Base
  {
    public string name { get; set; }
    public Property() { }

    [SchemaInfo("Property", "Creates a Speckle structural property", "Structural", "Properties")]
    public Property(string name)
    {
      this.name = name;
    }
  }
}

using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Properties;

public class Property : Base
{
  public Property() { }

  [SchemaInfo("Property", "Creates a Speckle structural property", "Structural", "Properties")]
  public Property(string name)
  {
    this.name = name;
  }

  public string name { get; set; }
}

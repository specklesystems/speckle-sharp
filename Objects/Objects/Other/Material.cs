using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Other;

/// <summary>
/// Generic class for materials containing generic parameters
/// </summary>
public class Material : Base
{
  public Material() { }

  [SchemaInfo("RevitMaterial", "Creates a Speckle material", "BIM", "Architecture")]
  public Material(string name)
  {
    this.name = name;
  }

  public string name { get; set; }
}

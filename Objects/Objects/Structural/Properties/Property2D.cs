using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Properties;

public class Property2D : Property
{
  public Property2D() { }

  [SchemaInfo("Property2D (by name)", "Creates a Speckle structural 2D element property", "Structural", "Properties")]
  public Property2D(string name)
  {
    this.name = name;
  }

  [SchemaInfo("Property2D", "Creates a Speckle structural 2D element property", "Structural", "Properties")]
  public Property2D(string name, StructuralMaterial? material, PropertyType2D type, double thickness)
  {
    this.name = name;
    this.material = material;
    this.type = type;
    this.thickness = thickness;
  }

  public PropertyType2D type { get; set; }
  public double thickness { get; set; } //also thickness type? ex. waffle vs constant

  [DetachProperty]
  public StructuralMaterial? material { get; set; }

  [DetachProperty]
  public Axis orientationAxis { get; set; }

  public ReferenceSurface refSurface { get; set; } //system plane
  public double zOffset { get; set; } //relative to reference surface
  public double modifierInPlane { get; set; }
  public double modifierBending { get; set; }
  public double modifierShear { get; set; }
  public double modifierVolume { get; set; }
  public string units { get; set; }
}

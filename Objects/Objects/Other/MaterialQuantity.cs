using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Other;

public class MaterialQuantity : Base
{
  public MaterialQuantity() { }

  [SchemaInfo("MaterialQuantity", "Creates the quantity of a material")]
  public MaterialQuantity(Material m, double volume, double area, string units)
  {
    material = m;
    this.volume = volume;
    this.area = area;
    this.units = units;
  }

  [DetachProperty]
  public Material material { get; set; }

  public double volume { get; set; }

  /// <summary>
  /// Area of the material on a element
  /// </summary>
  public double area { get; set; }

  /// <summary>
  /// UnitMeasure of the quantity,e.g meters implies squaremeters for area and cubicmeters for the volume
  /// </summary>
  public string units { get; set; }
}

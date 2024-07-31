using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Area : Base, IHasArea, IHasVolume, IDisplayValue<List<Base>>
{
  public Area() { }

  /// <summary>
  /// SchemaBuilder constructor for an Area
  /// </summary>
  [SchemaInfo("Area", "Creates a Speckle area", "BIM", "Other")]
  public Area(string name, string number, Level level, [SchemaMainParam] Point center)
  {
    this.name = name;
    this.number = number;
    this.level = level;
    this.center = center;
  }

  public string name { get; set; }
  public string number { get; set; }
  public Level level { get; set; }
  public Point center { get; set; }
  public List<ICurve> voids { get; set; } = new();
  public ICurve outline { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Base> displayValue { get; set; }

  public double area { get; set; }
  public double volume { get; set; }
}

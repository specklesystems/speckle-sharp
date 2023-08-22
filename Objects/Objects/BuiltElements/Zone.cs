using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class Zone : Base, IHasArea, IHasVolume
{
  public Zone() { }

  [SchemaInfo(
    "A zone is a collection of one or more spaces.",
    "Creates a Speckle zone to be used as a reference for spaces.",
    "BIM",
    "MEP"
  )]
  public Zone(string name)
  {
    this.name = name;
  }

  public string name { get; set; }

  public Level level { get; set; }

  public string serviceType { get; set; }

  public bool isDefault { get; set; }

  public string units { get; set; }

  // implicit measurements
  public double area { get; set; }
  public double volume { get; set; }

  public double perimeter { get; set; }
}

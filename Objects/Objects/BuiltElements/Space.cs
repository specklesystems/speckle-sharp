using System;
using System.Collections.Generic;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Space : Base, IHasArea, IHasVolume, IDisplayValue<List<Mesh>>
{
  public Space() { }

  [SchemaInfo("Space", "Creates a Speckle space", "BIM", "MEP")]
  public Space(string name, string number, [SchemaMainParam] Point basePoint, Level level)
  {
    this.name = name;
    this.number = number;
    this.basePoint = basePoint;
    this.level = level;
  }

  [SchemaInfo(
    "Space with top level and offset parameters",
    "Creates a Speckle space with the specified top level and offsets",
    "BIM",
    "MEP"
  )]
  public Space(
    string name,
    string number,
    [SchemaMainParam] Point basePoint,
    Level level,
    Level topLevel,
    double topOffset,
    double baseOffset
  )
  {
    this.name = name;
    this.number = number;
    this.basePoint = basePoint;
    this.level = level;
    this.topLevel = topLevel;
    this.topOffset = topOffset;
    this.baseOffset = baseOffset;
  }

  public string name { get; set; }
  public string number { get; set; }
  public Point basePoint { get; set; }
  public Level level { get; set; }
  public double baseOffset { get; set; }
  public Level topLevel { get; set; } // corresponds to UpperLimit property in Revit api
  public double topOffset { get; set; } // corresponds to LimitOffset property in Revit api
  public List<ICurve> voids { get; set; } = new();
  public ICurve outline { get; set; }
  public string spaceType { get; set; }

  // add the zone object for better forward compatibility
  public RevitZone? zone { get; set; }

  [Obsolete]
  public string zoneName { get; internal set; }
  public string units { get; set; }

  public string roomId { get; set; }

  public string phaseName { get; set; }

  // additional properties to add: also include space separation lines here?

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public double area { get; set; }
  public double volume { get; set; }
}

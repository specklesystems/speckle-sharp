using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Civil;

public class CivilCalculatedPoint : Base, ICivilCalculatedObject
{
  public CivilCalculatedPoint() { }

  public CivilCalculatedPoint(
    Point point,
    List<string> codes,
    Vector normalToBaseline,
    Vector normalToSubassembly,
    Point stationOffsetElevationToBaseline
  )
  {
    this.point = point;
    this.codes = codes;
    this.normalToBaseline = normalToBaseline;
    this.normalToSubassembly = normalToSubassembly;
    this.stationOffsetElevationToBaseline = stationOffsetElevationToBaseline;
  }

  public Point point { get; set; }

  public List<string> codes { get; set; }

  public Vector normalToBaseline { get; set; }

  public Vector normalToSubassembly { get; set; }

  public Point stationOffsetElevationToBaseline { get; set; }

  public string units { get; set; }
}

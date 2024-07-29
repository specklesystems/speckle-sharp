using System.Collections.Generic;
using Objects.Geometry;
using Objects.Other.Civil;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Civil;

public class CivilAppliedSubassembly : Base
{
  public CivilAppliedSubassembly() { }

  public CivilAppliedSubassembly(
    string subassemblyId,
    string subassemblyName,
    List<CivilCalculatedShape> shapes,
    Point stationOffsetElevationToBaseline,
    List<CivilDataField> parameters
  )
  {
    this.subassemblyId = subassemblyId;
    this.subassemblyName = subassemblyName;
    this.shapes = shapes;
    this.stationOffsetElevationToBaseline = stationOffsetElevationToBaseline;
    this.parameters = parameters;
  }

  public string subassemblyId { get; set; }

  public string subassemblyName { get; set; }

  public List<CivilCalculatedShape> shapes { get; set; }

  public Point stationOffsetElevationToBaseline { get; set; }

  [DetachProperty]
  public List<CivilDataField> parameters { get; set; }

  public string units { get; set; }
}

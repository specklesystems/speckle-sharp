using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Loading;

public class GSALoadBeam : LoadBeam
{
  public GSALoadBeam() { }

  [SchemaInfo("GSALoadBeam", "Creates a Speckle structural beam (1D elem/member) load for GSA", "GSA", "Loading")]
  public GSALoadBeam(
    int nativeId,
    LoadCase loadCase,
    List<Base> elements,
    BeamLoadType loadType,
    LoadDirection direction,
    LoadAxisType loadAxisType = LoadAxisType.Global,
    [SchemaParamInfo(
      "A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)"
    )]
      List<double> values = null,
    [SchemaParamInfo(
      "A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)"
    )]
      List<double> positions = null,
    bool isProjected = false
  )
  {
    this.nativeId = nativeId;
    this.loadCase = loadCase;
    this.elements = elements;
    this.loadType = loadType;
    this.direction = direction;
    this.loadAxisType = loadAxisType;
    this.values = values;
    this.positions = positions;
    this.isProjected = isProjected;
  }

  [SchemaInfo(
    "GSALoadBeam (user-defined axis)",
    "Creates a Speckle structural beam (1D elem/member) load (specified for a user-defined axis) for GSA",
    "GSA",
    "Loading"
  )]
  public GSALoadBeam(
    int nativeId,
    LoadCase loadCase,
    List<Base> elements,
    BeamLoadType loadType,
    LoadDirection direction,
    Axis loadAxis,
    [SchemaParamInfo(
      "A list that represents load magnitude (number of values varies based on load type - Point: 1, Uniform: 1, Linear: 2, Patch: 2, Tri-linear:2)"
    )]
      List<double> values = null,
    [SchemaParamInfo(
      "A list that represents load locations (number of values varies based on load type - Point: 1, Uniform: null, Linear: null, Patch: 2, Tri-linear: 2)"
    )]
      List<double> positions = null,
    bool isProjected = false
  )
  {
    this.nativeId = nativeId;
    this.loadCase = loadCase;
    this.elements = elements;
    this.loadType = loadType;
    this.direction = direction;
    this.values = values;
    this.positions = positions;
    this.isProjected = isProjected;
    this.nativeId = nativeId;
  }

  public int nativeId { get; set; }
}

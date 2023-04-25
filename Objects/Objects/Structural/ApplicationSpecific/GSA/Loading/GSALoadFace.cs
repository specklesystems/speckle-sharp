using System.Collections.Generic;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Loading;

public class GSALoadFace : LoadFace
{
  public GSALoadFace() { }

  [SchemaInfo("GSALoadFace", "Creates a Speckle structural face (2D elem/member) load for GSA", "GSA", "Loading")]
  public GSALoadFace(
    int nativeId,
    LoadCase loadCase,
    List<Base> elements,
    FaceLoadType loadType,
    LoadDirection2D direction,
    LoadAxisType loadAxisType = LoadAxisType.Global,
    [SchemaParamInfo(
      "A list that represents load magnitude (number of values varies based on load type - Uniform: 1, Variable: 4 (corner nodes), Point: 1)"
    )]
      List<double> values = null,
    [SchemaParamInfo(
      "A list that represents load locations (number of values varies based on load type - Uniform: null, Variable: null, Point: 2)"
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

  public int nativeId { get; set; }
}

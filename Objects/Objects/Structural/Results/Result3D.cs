using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Results;

public class ResultSet3D : Result
{
  public ResultSet3D() { }

  [SchemaInfo("ResultSet3D", "Creates a Speckle 3D element result set object", "Structural", "Results")]
  public ResultSet3D(List<Result3D> results3D)
  {
    this.results3D = results3D;
  }

  [DetachProperty]
  public List<Result3D> results3D { get; set; }
}

public class Result3D : Result
{
  public Result3D() { }

  [SchemaInfo(
    "Result3D (load case)",
    "Creates a Speckle 3D element result object (for load case)",
    "Structural",
    "Results"
  )]
  public Result3D(
    Element3D element,
    LoadCase resultCase,
    List<double> position,
    float dispX,
    float dispY,
    float dispZ,
    float stressXX,
    float stressYY,
    float stressZZ,
    float stressXY,
    float stressYZ,
    float stressZX
  )
  {
    this.element = element;
    this.resultCase = resultCase;
    this.position = position;
    this.dispX = dispX;
    this.dispY = dispY;
    this.dispZ = dispZ;
    this.stressXX = stressXX;
    this.stressYY = stressYY;
    this.stressZZ = stressZZ;
    this.stressXY = stressXY;
    this.stressYZ = stressYZ;
    this.stressZX = stressZX;
  }

  [SchemaInfo(
    "Result3D (load combination)",
    "Creates a Speckle 3D element result object (for load combination)",
    "Structural",
    "Results"
  )]
  public Result3D(
    Element3D element,
    LoadCombination resultCase,
    List<double> position,
    float dispX,
    float dispY,
    float dispZ,
    float stressXX,
    float stressYY,
    float stressZZ,
    float stressXY,
    float stressYZ,
    float stressZX
  )
  {
    this.element = element;
    this.resultCase = resultCase;
    this.position = position;
    this.dispX = dispX;
    this.dispY = dispY;
    this.dispZ = dispZ;
    this.stressXX = stressXX;
    this.stressYY = stressYY;
    this.stressZZ = stressZZ;
    this.stressXY = stressXY;
    this.stressYZ = stressYZ;
    this.stressZX = stressZX;
  }

  [DetachProperty]
  public Element3D element { get; set; }

  public List<double> position { get; set; } //relative position within element (x,y,z in range [0:1] to describe position)
  public float? dispX { get; set; }
  public float? dispY { get; set; }
  public float? dispZ { get; set; }
  public float? stressXX { get; set; }
  public float? stressYY { get; set; }
  public float? stressZZ { get; set; }
  public float? stressXY { get; set; }
  public float? stressYZ { get; set; }
  public float? stressZX { get; set; }
}

using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Results;

public class ResultSet2D : Result
{
  public ResultSet2D() { }

  [SchemaInfo("ResultSet2D", "Creates a Speckle 2D element result set object", "Structural", "Results")]
  public ResultSet2D(List<Result2D> results2D)
  {
    this.results2D = results2D;
  }

  [DetachProperty]
  public List<Result2D> results2D { get; set; }
}

public class Result2D : Result //result at a single position within a 2D element, ie. 2D element contains multiple Result2D objects to describe result at node 1, node 2, node 3, node4 and centre of 4-node quad element
{
  public Result2D() { }

  [SchemaInfo(
    "Result2D (load case)",
    "Creates a Speckle 2D element result object (for load case)",
    "Structural",
    "Results"
  )]
  public Result2D(
    Element2D element,
    LoadCase resultCase,
    List<double> position,
    float dispX,
    float dispY,
    float dispZ,
    float forceXX,
    float forceYY,
    float forceXY,
    float momentXX,
    float momentYY,
    float momentXY,
    float shearX,
    float shearY,
    float stressTopXX,
    float stressTopYY,
    float stressTopZZ,
    float stressTopXY,
    float stressTopYZ,
    float stressTopZX,
    float stressMidXX,
    float stressMidYY,
    float stressMidZZ,
    float stressMidXY,
    float stressMidYZ,
    float stressMidZX,
    float stressBotXX,
    float stressBotYY,
    float stressBotZZ,
    float stressBotXY,
    float stressBotYZ,
    float stressBotZX
  )
  {
    this.element = element;
    this.resultCase = resultCase;
    this.position = position;
    this.dispX = dispX;
    this.dispY = dispY;
    this.dispZ = dispZ;
    this.forceXX = forceXX;
    this.forceYY = forceYY;
    this.forceXY = forceXY;
    this.momentXX = momentXX;
    this.momentYY = momentYY;
    this.momentXY = momentXY;
    this.shearX = shearX;
    this.shearY = shearY;
    this.stressTopXX = stressTopXX;
    this.stressTopYY = stressTopYY;
    this.stressTopZZ = stressTopZZ;
    this.stressTopXY = stressTopXY;
    this.stressTopYZ = stressTopYZ;
    this.stressTopZX = stressTopZX;
    this.stressMidXX = stressMidXX;
    this.stressMidYY = stressMidYY;
    this.stressMidZZ = stressMidZZ;
    this.stressMidXY = stressMidXY;
    this.stressMidYZ = stressMidYZ;
    this.stressMidZX = stressMidZX;
    this.stressBotXX = stressBotXX;
    this.stressBotYY = stressBotYY;
    this.stressBotZZ = stressBotZZ;
    this.stressBotXY = stressBotXY;
    this.stressBotYZ = stressBotYZ;
    this.stressBotZX = stressBotZX;
  }

  [SchemaInfo(
    "Result2D (load combination)",
    "Creates a Speckle 2D element result object (for load combination)",
    "Structural",
    "Results"
  )]
  public Result2D(
    Element2D element,
    LoadCombination resultCase,
    List<double> position,
    float dispX,
    float dispY,
    float dispZ,
    float forceXX,
    float forceYY,
    float forceXY,
    float momentXX,
    float momentYY,
    float momentXY,
    float shearX,
    float shearY,
    float stressTopXX,
    float stressTopYY,
    float stressTopZZ,
    float stressTopXY,
    float stressTopYZ,
    float stressTopZX,
    float stressMidXX,
    float stressMidYY,
    float stressMidZZ,
    float stressMidXY,
    float stressMidYZ,
    float stressMidZX,
    float stressBotXX,
    float stressBotYY,
    float stressBotZZ,
    float stressBotXY,
    float stressBotYZ,
    float stressBotZX
  )
  {
    this.element = element;
    this.resultCase = resultCase;
    this.position = position;
    this.dispX = dispX;
    this.dispY = dispY;
    this.dispZ = dispZ;
    this.forceXX = forceXX;
    this.forceYY = forceYY;
    this.forceXY = forceXY;
    this.momentXX = momentXX;
    this.momentYY = momentYY;
    this.momentXY = momentXY;
    this.shearX = shearX;
    this.shearY = shearY;
    this.stressTopXX = stressTopXX;
    this.stressTopYY = stressTopYY;
    this.stressTopZZ = stressTopZZ;
    this.stressTopXY = stressTopXY;
    this.stressTopYZ = stressTopYZ;
    this.stressTopZX = stressTopZX;
    this.stressMidXX = stressMidXX;
    this.stressMidYY = stressMidYY;
    this.stressMidZZ = stressMidZZ;
    this.stressMidXY = stressMidXY;
    this.stressMidYZ = stressMidYZ;
    this.stressMidZX = stressMidZX;
    this.stressBotXX = stressBotXX;
    this.stressBotYY = stressBotYY;
    this.stressBotZZ = stressBotZZ;
    this.stressBotXY = stressBotXY;
    this.stressBotYZ = stressBotYZ;
    this.stressBotZX = stressBotZX;
  }

  [DetachProperty]
  public Element2D element { get; set; }

  public List<double> position { get; set; } //relative position within element (x,y in range [0:1], { 0.5, 0.5 } corresponds to centre of element, { 0, 0 } correponds to corner/at a node of a element
  public float? dispX { get; set; }
  public float? dispY { get; set; }
  public float? dispZ { get; set; }
  public float? forceXX { get; set; } //in-plane force per unit length in x direction
  public float? forceYY { get; set; } //in-plane force per unit length in y direction
  public float? forceXY { get; set; } //in-plane force per unit length in xy direction (at interface)
  public float? momentXX { get; set; } //moment per unit length in x direction
  public float? momentYY { get; set; } //moment per unit length in y direction
  public float? momentXY { get; set; } //moment per unit length in xy direction
  public float? shearX { get; set; } //through thickness shear force per unit length in x direction
  public float? shearY { get; set; } //through thickness shear force per unit length in y direction
  public float? stressTopXX { get; set; } //in-plane stress in x direction at top layer of element
  public float? stressTopYY { get; set; } //in-plane stress in y direction at top layer of element
  public float? stressTopZZ { get; set; } //in-plane stress in z direction (through thickness) at top layer of element
  public float? stressTopXY { get; set; } //shear stress in xy direction at top layer of element
  public float? stressTopYZ { get; set; } //shear stress in yz direction at top layer of element
  public float? stressTopZX { get; set; } //shear stress in zx direction at top layer of element
  public float? stressMidXX { get; set; } //in-plane stress in x direction at mid layer of element
  public float? stressMidYY { get; set; } //in-plane stress in y direction at mid layer of element
  public float? stressMidZZ { get; set; } //in-plane stress in z direction (through thickness) at mid layer of element
  public float? stressMidXY { get; set; } //shear stress in xy direction at mid layer of element
  public float? stressMidYZ { get; set; } //shear stress in yz direction at mid layer of element
  public float? stressMidZX { get; set; } //shear stress in zx direction at mid layer of element
  public float? stressBotXX { get; set; } //in-plane stress in x direction at bot layer of element
  public float? stressBotYY { get; set; } //in-plane stress in y direction at bot layer of element
  public float? stressBotZZ { get; set; } //in-plane stress in z direction (through thickness) at bot layer of element
  public float? stressBotXY { get; set; } //shear stress in xy direction at bot layer of element
  public float? stressBotYZ { get; set; } //shear stress in yz direction at bot layer of element
  public float? stressBotZX { get; set; } //shear stress in zx direction at bot layer of element
}

using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Results;

public class ResultSet1D : Result
{
  public ResultSet1D() { }

  [SchemaInfo("ResultSet1D", "Creates a Speckle 1D element result set object", "Structural", "Results")]
  public ResultSet1D(List<Result1D> results1D)
  {
    this.results1D = results1D;
  }

  [DetachProperty]
  public List<Result1D> results1D { get; set; }
}

public class Result1D : Result //result at a single position along a 1D element, ie. 1D element contains multiple Result1D objects to describe result at end 1, mid-span, end 2
{
  public Result1D() { }

  [SchemaInfo(
    "Result1D (load case)",
    "Creates a Speckle 1D element result object (for load case)",
    "Structural",
    "Results"
  )]
  public Result1D(
    Element1D element,
    LoadCase resultCase,
    float position,
    float dispX,
    float dispY,
    float dispZ,
    float rotXX,
    float rotYY,
    float rotZZ,
    float forceX,
    float forceY,
    float forceZ,
    float momentXX,
    float momentYY,
    float momentZZ,
    float axialStress,
    float shearStressY,
    float shearStressZ,
    float bendingStressYPos,
    float bendingStressYNeg,
    float bendingStressZPos,
    float bendingStressZNeg,
    float combinedStressMax,
    float combinedStressMin
  )
  {
    this.element = element;
    this.resultCase = resultCase;
    this.position = position;
    this.dispX = dispX;
    this.dispY = dispY;
    this.dispZ = dispZ;
    this.rotXX = rotXX;
    this.rotYY = rotYY;
    this.rotZZ = rotZZ;
    this.forceX = forceX;
    this.forceY = forceY;
    this.forceZ = forceZ;
    this.momentXX = momentXX;
    this.momentYY = momentYY;
    this.momentZZ = momentZZ;
    this.axialStress = axialStress;
    this.shearStressY = shearStressY;
    this.shearStressZ = shearStressZ;
    this.bendingStressYPos = bendingStressYPos;
    this.bendingStressYNeg = bendingStressYNeg;
    this.bendingStressZPos = bendingStressZPos;
    this.bendingStressZNeg = bendingStressZNeg;
    this.combinedStressMax = combinedStressMax;
    this.combinedStressMin = combinedStressMin;
  }

  [SchemaInfo(
    "Result1D (load combination)",
    "Creates a Speckle 1D element result object (for load combination)",
    "Structural",
    "Results"
  )]
  public Result1D(
    Element1D element,
    LoadCombination resultCase,
    float position,
    float dispX,
    float dispY,
    float dispZ,
    float rotXX,
    float rotYY,
    float rotZZ,
    float forceX,
    float forceY,
    float forceZ,
    float momentXX,
    float momentYY,
    float momentZZ,
    float axialStress,
    float shearStressY,
    float shearStressZ,
    float bendingStressYPos,
    float bendingStressYNeg,
    float bendingStressZPos,
    float bendingStressZNeg,
    float combinedStressMax,
    float combinedStressMin
  )
  {
    this.element = element;
    this.resultCase = resultCase;
    this.position = position;
    this.dispX = dispX;
    this.dispY = dispY;
    this.dispZ = dispZ;
    this.rotXX = rotXX;
    this.rotYY = rotYY;
    this.rotZZ = rotZZ;
    this.forceX = forceX;
    this.forceY = forceY;
    this.forceZ = forceZ;
    this.momentXX = momentXX;
    this.momentYY = momentYY;
    this.momentZZ = momentZZ;
    this.axialStress = axialStress;
    this.shearStressY = shearStressY;
    this.shearStressZ = shearStressZ;
    this.bendingStressYPos = bendingStressYPos;
    this.bendingStressYNeg = bendingStressYNeg;
    this.bendingStressZPos = bendingStressZPos;
    this.bendingStressZNeg = bendingStressZNeg;
    this.combinedStressMax = combinedStressMax;
    this.combinedStressMin = combinedStressMin;
  }

  [DetachProperty]
  public Element1D element { get; set; }

  public float? position { get; set; } //location along 1D element, normalised position (from 0 for end 1 to 1 for end 2)
  public float? dispX { get; set; }
  public float? dispY { get; set; }
  public float? dispZ { get; set; }
  public float? rotXX { get; set; }
  public float? rotYY { get; set; }
  public float? rotZZ { get; set; }
  public float? forceX { get; set; }
  public float? forceY { get; set; }
  public float? forceZ { get; set; }
  public float? momentXX { get; set; }
  public float? momentYY { get; set; }
  public float? momentZZ { get; set; }
  public float? axialStress { get; set; } //axial stress, ie. Fx/Area
  public float? shearStressY { get; set; } //shear stress, in minor axis dir, ie. Fy/Area
  public float? shearStressZ { get; set; } //shear stress, in major axis dir, ie. Fz/Area
  public float? bendingStressYPos { get; set; } //bending stress, about minor axis, ie. Myy/Iyy x Dz (Dz as distance from the centroid to the edge of the section in the +ve z direction)
  public float? bendingStressYNeg { get; set; } //bending stress, about minor axis, ie. Myy/Iyy x Dz (Dz as distance from the centroid to the edge of the section in the -ve z direction)
  public float? bendingStressZPos { get; set; } //bending stress, about major axis, ie. -Mzz/Izz x Dy (Dy as distance from the centroid to the edge of the section in the +ve y direction)
  public float? bendingStressZNeg { get; set; } //bending stress, about major axis, ie. -Mzz/Izz x Dy (Dy as distance from the centroid to the edge of the section in the -ve y direction)
  public float? combinedStressMax { get; set; } //maximum extreme fibre longitudinal stress due to axial forces and transverse bending
  public float? combinedStressMin { get; set; } //minimum extreme fibre longitudinal stress due to axial forces and transverse bending
}

using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.Results
{
  public class LoadCombinationResult1D : Result
  {
    public LoadCombinationResult1D() { }

    [SchemaInfo("ResultGroup1D", "Creates a Speckle 1D element result group object", "Structural", "Results")]
    public LoadCombinationResult1D(Base loadCase, List<AnalyticalResult1D> results1D)
    {
      this.resultCase = loadCase;
      this.results1D = results1D;
    }

    [Chunkable]
    public List<AnalyticalResult1D> results1D { get; set; }
  }

  public class AnalyticalResult1D : Base
  {
    public float? positionAlongBeam { get; set; }
    public float? axialForce { get; set; }
    public float? shearForceStrongAxis { get; set; }
    public float? shearForceWeakAxis { get; set; }
    public float? torsionForce { get; set; }
    public float? momentAboutStrongAxis { get; set; }
    public float? momentAboutWeakAxis { get; set; }
  }

}

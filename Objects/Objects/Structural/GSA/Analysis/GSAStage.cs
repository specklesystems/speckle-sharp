using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Analysis;

public class GSAStage : Base
{
  public GSAStage() { }

  [SchemaInfo("GSAStage", "Creates a Speckle structural analysis stage for GSA", "GSA", "Analysis")]
  public GSAStage(
    int nativeId,
    string name,
    string colour,
    List<Base> elements,
    double creepFactor,
    int stageTime,
    List<Base> lockedElements
  )
  {
    this.nativeId = nativeId;
    this.name = name;
    this.colour = colour;
    this.elements = elements;
    this.creepFactor = creepFactor;
    this.stageTime = stageTime;
    this.lockedElements = lockedElements;
  }

  public int nativeId { get; set; }
  public string name { get; set; }
  public string colour { get; set; }

  [DetachProperty, Chunkable(5000)]
  public List<Base> elements { get; set; }

  public double creepFactor { get; set; } //Phi
  public int stageTime { get; set; } //number of days

  [DetachProperty, Chunkable(5000)]
  public List<Base> lockedElements { get; set; } //elements not part of the current analysis stage
}

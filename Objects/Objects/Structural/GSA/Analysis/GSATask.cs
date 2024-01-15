using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Analysis;

public class GSATask : Base
{
  public GSATask() { }

  [SchemaInfo("GSAAnalysisTask", "Creates a Speckle structural analysis task for GSA", "GSA", "Analysis")]
  public GSATask(int nativeId, string name)
  {
    this.nativeId = nativeId;
    this.name = name;
  }

  public int nativeId { get; set; } //equiv to num
  public string name { get; set; }
  public string stage { get; set; }
  public string solver { get; set; }
  public SolutionType solutionType { get; set; }
  public int modeParameter1 { get; set; } //start mode
  public int modeParameter2 { get; set; } //number of modes
  public int numIterations { get; set; }
  public string PDeltaOption { get; set; }
  public string PDeltaCase { get; set; }
  public string PrestressCase { get; set; }
  public string resultSyntax { get; set; }
  public PruningOption prune { get; set; }
}

public enum SolutionType
{
  Undefined, //no solution specified
  Static,
  Modal,
  Ritz,
  Buckling,
  StaticPDelta,
  ModalPDelta,
  RitzPDelta,
  Mass,
  Stability,
  StabilityPDelta,
  BucklingNonLinear,
  Influence
}

public enum PruningOption
{
  None,
  Influence
}

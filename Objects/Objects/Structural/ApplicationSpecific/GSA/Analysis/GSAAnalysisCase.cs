using System;
using System.Collections.Generic;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Analysis;

public class GSAAnalysisCase : Base
{
  public GSAAnalysisCase() { }

  [SchemaInfo("GSAAnalysisCase", "Creates a Speckle structural analysis case for GSA", "GSA", "Analysis")]
  public GSAAnalysisCase(
    int nativeId,
    string name,
    GSATask task,
    [SchemaParamInfo("A list of load cases")] List<LoadCase> loadCases,
    [SchemaParamInfo("A list of load factors (to be mapped to provided load cases)")] List<double> loadFactors
  )
  {
    if (loadCases.Count != loadFactors.Count)
      throw new ArgumentException("Number of load cases provided does not match number of load factors provided");
    this.nativeId = nativeId;
    this.name = name;
    this.task = task;
    this.loadCases = loadCases;
    this.loadFactors = loadFactors;
  }

  public int nativeId { get; set; }
  public string name { get; set; }

  [DetachProperty]
  public GSATask task { get; set; } //task reference

  [DetachProperty]
  public List<LoadCase> loadCases { get; set; }

  public List<double> loadFactors { get; set; }
}

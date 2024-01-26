using System;
using System.Collections.Generic;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.GSA.Loading;

public class GSALoadCombination : LoadCombination
{
  public GSALoadCombination() { }

  [SchemaInfo("GSALoadCombination", "Creates a Speckle load combination for GSA", "GSA", "Loading")]
  public GSALoadCombination(
    int nativeId,
    string name,
    [SchemaParamInfo("A list of load cases")] List<Base> loadCases,
    [SchemaParamInfo("A list of load factors (to be mapped to provided load cases)")] List<double> loadFactors
  )
  {
    this.nativeId = nativeId;
    this.name = name;

    if (loadCases.Count != loadFactors.Count)
    {
      throw new ArgumentException("Number of load cases provided does not match number of load factors provided");
    }

    this.loadFactors = loadFactors;
    this.loadCases = loadCases;
    this.nativeId = nativeId;
  }

  public int nativeId { get; set; }
}

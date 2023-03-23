using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Loading;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.Results
{
  public class Result : Base
  {
    [DetachProperty]
    public Base resultCase { get; set; } //loadCase or loadCombination
    public string permutation { get; set; } //for enveloped cases?
    public string description { get; set; }
    public Result() { }

    public Result(LoadCase resultCase, string description = null)
    {
      this.resultCase = resultCase;
      this.description = description;
    }

    public Result(LoadCombination resultCase, string description = null)
    {
      this.resultCase = resultCase;
      this.description = description;
    }
  }
}

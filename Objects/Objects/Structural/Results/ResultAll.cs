using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;


namespace Objects.Structural.Results
{
  public class ResultSetAll : Base
  {
    [DetachProperty]
    public ResultSet1D results1D { get; set; } //1d element results

    [DetachProperty]
    public ResultSet2D results2D { get; set; } //2d elements results

    [DetachProperty]
    public ResultSet3D results3D { get; set; } //3d elements results

    [DetachProperty]
    public ResultGlobal resultsGlobal { get; set; } //global results

    [DetachProperty]
    public ResultSetNode resultsNode { get; set; } //nodal results

    public ResultSetAll() { }

    [SchemaInfo("ResultSetAll", "Creates a Speckle result set object for 1d element, 2d element, 3d element global and nodal results", "Structural", "Results")]
    public ResultSetAll(ResultSet1D results1D, ResultSet2D results2D, ResultSet3D results3D, ResultGlobal resultsGlobal, ResultSetNode resultsNode)
    {
      this.results1D = results1D;
      this.results2D = results2D;
      this.results3D = results3D;
      this.resultsGlobal = resultsGlobal;
      this.resultsNode = resultsNode;
    }
  }
}

using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Loading;

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

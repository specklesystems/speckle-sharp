using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;

namespace Objects.Structural.Results
{
    public class ResultSet3D : Result
    {
        [DetachProperty]
        public List<Result3D> results3D { get; set; }
        public ResultSet3D() { }
        [SchemaInfo("ResultSet3D", "Creates a Speckle 3D element result set object", "Structural", "Results")]
        public ResultSet3D(List<Result3D> results3D)
        {
            this.results3D = results3D;
        }
    }

    public class Result3D : Result
    {
        [DetachProperty]
        public Element3D element { get; set; }
        public List<double> position { get; set; } //relative position within element (x,y,z in range [0:1] to describe position)
        public double dispX { get; set; }
        public double dispY { get; set; }
        public double dispZ { get; set; }
        public double stressXX { get; set; } 
        public double stressYY { get; set; }
        public double stressZZ { get; set; } 
        public double stressXY { get; set; } 
        public double stressYZ { get; set; }
        public double stressZX { get; set; }
        public Result3D() { }

        [SchemaInfo("Result3D (load case)", "Creates a Speckle 3D element result object (for load case)", "Structural", "Results")]
        public Result3D(Element3D element, LoadCase resultCase, List<double> position, double dispX, double dispY, double dispZ, double stressXX, double stressYY, double stressZZ, double stressXY, double stressYZ, double stressZX)
        {
            this.element = element;
            this.resultCase = resultCase;
            this.position = position;
            this.dispX = dispX;
            this.dispY = dispY;
            this.dispZ = dispZ;
            this.stressXX = stressXX;
            this.stressYY = stressYY;
            this.stressZZ = stressZZ;
            this.stressXY = stressXY;
            this.stressYZ = stressYZ;
            this.stressZX = stressZX;
        }

        [SchemaInfo("Result3D (load combination)", "Creates a Speckle 3D element result object (for load combination)", "Structural", "Results")]
        public Result3D(Element3D element, LoadCombination resultCase, List<double> position, double dispX, double dispY, double dispZ, double stressXX, double stressYY, double stressZZ, double stressXY, double stressYZ, double stressZX)
        {
            this.element = element;
            this.resultCase = resultCase;
            this.position = position;
            this.dispX = dispX;
            this.dispY = dispY;
            this.dispZ = dispZ;
            this.stressXX = stressXX;
            this.stressYY = stressYY;
            this.stressZZ = stressZZ;
            this.stressXY = stressXY;
            this.stressYZ = stressYZ;
            this.stressZX = stressZX;
        }

    }

}

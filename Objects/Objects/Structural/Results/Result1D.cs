using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;

namespace Objects.Structural.Results
{
    public class ResultSet1D : Result
    {
        [DetachProperty]
        public List<Result1D> results1D { get; set; }
        public ResultSet1D() { }

        [SchemaInfo("ResultSet1D", "Creates a Speckle 1D element result set object", "Structural", "Results")]
        public ResultSet1D(List<Result1D> results1D)
        {
            this.results1D = results1D;
        }        
    }
    
    public class Result1D : Result //result at a single position along a 1D element, ie. 1D element contains multiple Result1D objects to describe result at end 1, mid-span, end 2
    {
        [DetachProperty]
        public Element1D element { get; set; }
        public double position { get; set; } //location along 1D element, normalised position (from 0 for end 1 to 1 for end 2)
        public double dispX { get; set; }
        public double dispY { get; set; }
        public double dispZ { get; set; }
        public double rotXX { get; set; }
        public double rotYY { get; set; }
        public double rotZZ { get; set; }
        public double forceX { get; set; }
        public double forceY { get; set; }
        public double forceZ { get; set; }
        public double momentXX { get; set; }
        public double momentYY { get; set; }
        public double momentZZ { get; set; }
        public double axialStress { get; set; } //axial stress, ie. Fx/Area
        public double shearStressY { get; set; } //shear stress, in minor axis dir, ie. Fy/Area
        public double shearStressZ { get; set; } //shear stress, in major axis dir, ie. Fz/Area
        public double bendingStressYPos { get; set; } //bending stress, about minor axis, ie. Myy/Iyy x Dz (Dz as distance from the centroid to the edge of the section in the +ve z direction)
        public double bendingStressYNeg { get; set; } //bending stress, about minor axis, ie. Myy/Iyy x Dz (Dz as distance from the centroid to the edge of the section in the -ve z direction)
        public double bendingStressZPos { get; set; } //bending stress, about major axis, ie. -Mzz/Izz x Dy (Dy as distance from the centroid to the edge of the section in the +ve y direction)
        public double bendingStressZNeg { get; set; } //bending stress, about major axis, ie. -Mzz/Izz x Dy (Dy as distance from the centroid to the edge of the section in the -ve y direction)
        public double combinedStressMax { get; set; } //maximum extreme fibre longitudinal stress due to axial forces and transverse bending
        public double combinedStressMin { get; set; } //minimum extreme fibre longitudinal stress due to axial forces and transverse bending
        public Result1D() { }

        [SchemaInfo("Result1D (load case)", "Creates a Speckle 1D element result object (for load case)", "Structural", "Results")]
        public Result1D(Element1D element, LoadCase resultCase, double position, double dispX, double dispY, double dispZ, double rotXX, double rotYY, double rotZZ, double forceX, double forceY, double forceZ, double momentXX, double momentYY, double momentZZ, double axialStress, double shearStressY, double shearStressZ, double bendingStressYPos, double bendingStressYNeg, double bendingStressZPos, double bendingStressZNeg, double combinedStressMax, double combinedStressMin)
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

        [SchemaInfo("Result1D (load combination)", "Creates a Speckle 1D element result object (for load combination)", "Structural", "Results")]
        public Result1D(Element1D element, LoadCombination resultCase, double position, double dispX, double dispY, double dispZ, double rotXX, double rotYY, double rotZZ, double forceX, double forceY, double forceZ, double momentXX, double momentYY, double momentZZ, double axialStress, double shearStressY, double shearStressZ, double bendingStressYPos, double bendingStressYNeg, double bendingStressZPos, double bendingStressZNeg, double combinedStressMax, double combinedStressMin)
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
    }
}

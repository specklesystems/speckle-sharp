using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;

namespace Objects.Structural.Results
{
    public class ResultSet2D : Result
    {
        [DetachProperty]
        public List<Result2D> results2D { get; set; }
        public ResultSet2D() { }

        [SchemaInfo("ResultSet2D", "Creates a Speckle 2D element result set object", "Structural", "Results")]
        public ResultSet2D(List<Result2D> results2D)
        {
            this.results2D = results2D;
        }
    }

    public class Result2D : Result //result at a single position within a 2D element, ie. 2D element contains multiple Result2D objects to describe result at node 1, node 2, node 3, node4 and centre of 4-node quad element
    {
        [DetachProperty]
        public Element2D element { get; set; }
        public List<double> position { get; set; } //relative position within element (x,y in range [0:1], { 0.5, 0.5 } corresponds to centre of element, { 0, 0 } correponds to corner/at a node of a element
        public double dispX { get; set; }
        public double dispY { get; set; }
        public double dispZ { get; set; }
        public double forceXX { get; set; } //in-plane force per unit length in x direction
        public double forceYY { get; set; } //in-plane force per unit length in y direction
        public double forceXY { get; set; } //in-plane force per unit length in xy direction (at interface)
        public double momentXX { get; set; } //moment per unit length in x direction
        public double momentYY { get; set; } //moment per unit length in y direction
        public double momentXY { get; set; } //moment per unit length in xy direction
        public double shearX { get; set; } //through thickness shear force per unit length in x direction
        public double shearY { get; set; } //through thickness shear force per unit length in y direction
        public double stressTopXX { get; set; } //in-plane stress in x direction at top layer of element
        public double stressTopYY { get; set; } //in-plane stress in y direction at top layer of element
        public double stressTopZZ { get; set; } //in-plane stress in z direction (through thickness) at top layer of element
        public double stressTopXY { get; set; } //shear stress in xy direction at top layer of element
        public double stressTopYZ { get; set; } //shear stress in yz direction at top layer of element
        public double stressTopZX { get; set; } //shear stress in zx direction at top layer of element
        public double stressMidXX { get; set; } //in-plane stress in x direction at mid layer of element
        public double stressMidYY { get; set; } //in-plane stress in y direction at mid layer of element
        public double stressMidZZ { get; set; } //in-plane stress in z direction (through thickness) at mid layer of element
        public double stressMidXY { get; set; } //shear stress in xy direction at mid layer of element
        public double stressMidYZ { get; set; } //shear stress in yz direction at mid layer of element
        public double stressMidZX { get; set; } //shear stress in zx direction at mid layer of element
        public double stressBotXX { get; set; } //in-plane stress in x direction at bot layer of element
        public double stressBotYY { get; set; } //in-plane stress in y direction at bot layer of element
        public double stressBotZZ { get; set; } //in-plane stress in z direction (through thickness) at bot layer of element
        public double stressBotXY { get; set; } //shear stress in xy direction at bot layer of element
        public double stressBotYZ { get; set; } //shear stress in yz direction at bot layer of element
        public double stressBotZX { get; set; } //shear stress in zx direction at bot layer of element
        public Result2D() { }

        [SchemaInfo("Result2D (load case)", "Creates a Speckle 2D element result object (for load case)", "Structural", "Results")]
        public Result2D(Element2D element, LoadCase resultCase, List<double> position, double dispX, double dispY, double dispZ, double forceXX, double forceYY, double forceXY, double momentXX, double momentYY, double momentXY, double shearX, double shearY, double stressTopXX, double stressTopYY, double stressTopZZ, double stressTopXY, double stressTopYZ, double stressTopZX, double stressMidXX, double stressMidYY, double stressMidZZ, double stressMidXY, double stressMidYZ, double stressMidZX, double stressBotXX, double stressBotYY, double stressBotZZ, double stressBotXY, double stressBotYZ, double stressBotZX)
        {            
            this.element = element;
            this.resultCase = resultCase;
            this.position = position;
            this.dispX = dispX;
            this.dispY = dispY;
            this.dispZ = dispZ;
            this.forceXX = forceXX;
            this.forceYY = forceYY;
            this.forceXY = forceXY;
            this.momentXX = momentXX;
            this.momentYY = momentYY;
            this.momentXY = momentXY;
            this.shearX = shearX;
            this.shearY = shearY;
            this.stressTopXX = stressTopXX;
            this.stressTopYY = stressTopYY;
            this.stressTopZZ = stressTopZZ;
            this.stressTopXY = stressTopXY;
            this.stressTopYZ = stressTopYZ;
            this.stressTopZX = stressTopZX;
            this.stressMidXX = stressMidXX;
            this.stressMidYY = stressMidYY;
            this.stressMidZZ = stressMidZZ;
            this.stressMidXY = stressMidXY;
            this.stressMidYZ = stressMidYZ;
            this.stressMidZX = stressMidZX;
            this.stressBotXX = stressBotXX;
            this.stressBotYY = stressBotYY;
            this.stressBotZZ = stressBotZZ;
            this.stressBotXY = stressBotXY;
            this.stressBotYZ = stressBotYZ;
            this.stressBotZX = stressBotZX;
        }

        [SchemaInfo("Result2D (load combination)", "Creates a Speckle 2D element result object (for load combination)", "Structural", "Results")]
        public Result2D(Element2D element, LoadCombination resultCase, List<double> position, double dispX, double dispY, double dispZ, double forceXX, double forceYY, double forceXY, double momentXX, double momentYY, double momentXY, double shearX, double shearY, double stressTopXX, double stressTopYY, double stressTopZZ, double stressTopXY, double stressTopYZ, double stressTopZX, double stressMidXX, double stressMidYY, double stressMidZZ, double stressMidXY, double stressMidYZ, double stressMidZX, double stressBotXX, double stressBotYY, double stressBotZZ, double stressBotXY, double stressBotYZ, double stressBotZX)
        {
            this.element = element;
            this.resultCase = resultCase;
            this.position = position;
            this.dispX = dispX;
            this.dispY = dispY;
            this.dispZ = dispZ;
            this.forceXX = forceXX;
            this.forceYY = forceYY;
            this.forceXY = forceXY;
            this.momentXX = momentXX;
            this.momentYY = momentYY;
            this.momentXY = momentXY;
            this.shearX = shearX;
            this.shearY = shearY;
            this.stressTopXX = stressTopXX;
            this.stressTopYY = stressTopYY;
            this.stressTopZZ = stressTopZZ;
            this.stressTopXY = stressTopXY;
            this.stressTopYZ = stressTopYZ;
            this.stressTopZX = stressTopZX;
            this.stressMidXX = stressMidXX;
            this.stressMidYY = stressMidYY;
            this.stressMidZZ = stressMidZZ;
            this.stressMidXY = stressMidXY;
            this.stressMidYZ = stressMidYZ;
            this.stressMidZX = stressMidZX;
            this.stressBotXX = stressBotXX;
            this.stressBotYY = stressBotYY;
            this.stressBotZZ = stressBotZZ;
            this.stressBotXY = stressBotXY;
            this.stressBotYZ = stressBotYZ;
            this.stressBotZX = stressBotZX;
        }
    }

}

using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.Geometry
{
    public class Element2D : Base, IDisplayMesh
    {        
        public string name { get; set; }
        public Mesh baseMesh { get; set; } //rhino - parent mesh? elements (including props/materias) explicitly defined in a list

        [DetachProperty]
        public Property2D property { get; set; }
        public ElementType2D type { get; set; }
        public double offset { get; set; } //z direction (normal)
        public double orientationAngle { get; set; }

        [DetachProperty]
        public Base parent { get; set; } //parent element

        [DetachProperty]
        public List<Node> topology { get; set; }

        [DetachProperty]
        public Mesh displayMesh { get; set; }
        public string units { get; set; }

        public Element2D() { }
        public Element2D(Mesh baseMesh)
        {
            this.baseMesh = baseMesh;
        }

        [SchemaInfo("Element2D", "Creates a Speckle structural 2D element", "Structural", "Geometry")]
        public Element2D(Mesh baseMesh, Property2D property, ElementType2D type, double offset = 0, double orientationAngle = 0)
        {
            this.baseMesh = baseMesh;
            this.property = property;
            this.type = type; //derive from geom? 
            this.offset = offset;
            this.orientationAngle = orientationAngle;
        }
    }
}
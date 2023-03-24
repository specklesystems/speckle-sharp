using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.Geometry
{
  public class Element3D : Base
  {
    public string name { get; set; }
    public Mesh baseMesh { get; set; } //rhino - parent mesh? elements (including props/materias) explicitly defined in a list

    [DetachProperty]
    public Property3D property { get; set; }
    public ElementType3D type { get; set; }
    public double orientationAngle { get; set; }

    [DetachProperty]
    public Base parent { get; set; } //parent element

    [DetachProperty]
    public List<Node> topology { get; set; }
    public string units { get; set; }

    public Element3D() { }
    public Element3D(Mesh baseMesh)
    {
      this.baseMesh = baseMesh;
    }

    [SchemaInfo("Element3D", "Creates a Speckle structural 3D element", "Structural", "Geometry")]
    public Element3D(Mesh baseMesh, Property3D property, ElementType3D type, string name = null, double orientationAngle = 0)
    {
      this.baseMesh = baseMesh;
      this.property = property;
      this.type = type;
      this.name = name;
      this.orientationAngle = orientationAngle;
    }
  }
}

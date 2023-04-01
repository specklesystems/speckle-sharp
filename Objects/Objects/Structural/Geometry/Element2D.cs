using System;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.Geometry
{
  public class Element2D : Base, IDisplayValue<List<Mesh>>
  {
    public string name { get; set; }

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
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Element2D() { }

    public Element2D(List<Node> nodes)
    {
      this.topology = nodes;
    }

    [SchemaInfo("Element2D", "Creates a Speckle structural 2D element (based on a list of edge ie. external, geometry defining nodes)", "Structural", "Geometry")]
    public Element2D(List<Node> nodes, Property2D property, double offset = 0, double orientationAngle = 0)
    {
      this.topology = nodes;
      this.property = property;
      this.offset = offset;
      this.orientationAngle = orientationAngle;
    }
  }
}
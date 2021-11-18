using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Topography : Base, IDisplayMesh
  {
    public Mesh baseGeometry { get; set; } = new Mesh();

    [DetachProperty]
    public Mesh displayMesh { get; set; } = new Mesh();

    public string units { get; set; }

    public Topography() { }

    [SchemaInfo("Topography", "Creates a Speckle topography", "BIM", "Architecture")]
    public Topography([SchemaMainParam] Mesh displayMesh)
    {
      this.displayMesh = displayMesh;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitTopography : Topography
  {
    public string elementId { get; set; }
    public Base parameters { get; set; }

    public RevitTopography() { }

    [SchemaInfo("RevitTopography", "Creates a Revit topography", "Revit", "Architecture")]
    public RevitTopography([SchemaMainParam] Mesh displayMesh, List<Parameter> parameters = null)
    {
      this.displayMesh = displayMesh;
      this.parameters = parameters.ToBase();
    }
  }
}

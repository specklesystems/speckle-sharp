using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Topography : Base
  {
    public Mesh baseGeometry { get; set; } = new Mesh();
    public Topography() { }

    [SchemaInfo("Topography", "Creates a Speckle topography")]
    public Topography(Mesh baseGeometry)
    {
      this.baseGeometry = baseGeometry;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitTopography : Topography
  {
    public string elementId { get; set; }

    public List<Parameter> parameters { get; set; }
    public RevitTopography()
    {

    }

    [SchemaInfo("RevitTopography", "Creates a Revit topography")]
    public RevitTopography(Mesh baseGeometry, List<Parameter> parameters = null)
    {
      this.baseGeometry = baseGeometry;
      this.parameters = parameters;
    }
  }

}

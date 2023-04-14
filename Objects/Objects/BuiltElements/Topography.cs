using System.Collections.Generic;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Topography : Base, IDisplayValue<List<Mesh>>
  {
    public Topography()
    {
      displayValue = new List<Mesh>();
    }

    [SchemaInfo("Topography", "Creates a Speckle topography", "BIM", "Architecture")]
    public Topography([SchemaMainParam] Mesh displayMesh)
    {
      displayValue = new List<Mesh> { displayMesh };
    }

    public Mesh baseGeometry { get; set; } = new();

    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    //TODO Figure out if we should add a new constructor that takes a List<Mesh> or if Topography should just have a single mesh display value
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitTopography : Topography
  {
    public RevitTopography() { }

    [SchemaInfo("RevitTopography", "Creates a Revit topography", "Revit", "Architecture")]
    public RevitTopography([SchemaMainParam] Mesh displayMesh, List<Parameter> parameters = null)
    {
      displayValue = new List<Mesh> { displayMesh };
      this.parameters = parameters.ToBase();
    }

    public string elementId { get; set; }
    public Base parameters { get; set; }
  }
}

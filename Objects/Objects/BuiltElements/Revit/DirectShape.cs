using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class DirectShape : Base , IDisplayMesh
  {
    public string name { get; set; }
    public RevitCategory category { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    [DetachProperty]
    public List<Base> baseGeometries { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public DirectShape() { }

    /// <summary>
    ///  Constructs a new <see cref="DirectShape"/> instance given a list of <see cref="Base"/> objects.
    /// </summary>
    /// <param name="name">The name of the <see cref="DirectShape"/></param>
    /// <param name="category">The <see cref="RevitCategory"/> of this instance.</param>
    /// <param name="baseGeometries">A list of base classes to represent the direct shape (only mesh and brep are allowed, anything else will be ignored.)</param>
    /// <param name="parameters">Optional Parameters for this instance.</param>
    [SchemaInfo("DirectShape by base geometries", "Creates a Revit DirectShape using a list of base geometry objects.")]
    public DirectShape(string name, RevitCategory category, List<Base> baseGeometries, List<Parameter> parameters = null)
    {
      this.name = name;
      this.category = category;
      this.baseGeometries = baseGeometries.FindAll(IsValidObject);
      this.parameters = parameters;
    }

    public bool IsValidObject(Base @base) =>
      @base is Point
      || @base is ICurve
      || @base is Mesh
      || @base is Brep;
  }
}

using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using static Objects.BuiltElements.Revit.RevitUtils;

namespace Objects.BuiltElements.Revit
{
  public class DirectShape : Base
  {
    public string type { get; set; }

    public RevitCategory category { get; set; }

    public Mesh baseGeometry { get; set; }
    
    public Brep solidGeometry { get; set; }

    public List<Parameter> parameters { get; set; }
    
    public string elementId { get; set; }

    public bool isSolid => solidGeometry != null;
    
    public DirectShape()
    { }

    [SchemaInfo("DirectShape by mesh", "Creates a Revit DirectShape by mesh")]
    public DirectShape(string type, RevitCategory category, Mesh baseGeometry, List<Parameter> parameters = null)
    {
      this.type = type;
      this.category = category;
      this.baseGeometry = baseGeometry;
      this.parameters = parameters;
    }

  }

}

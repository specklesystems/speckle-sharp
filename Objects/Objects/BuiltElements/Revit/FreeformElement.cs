using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class FreeformElement : Base , IDisplayMesh
  {
    public List<Parameter> parameters { get; set; }
    
    public string elementId { get; set; }

    [DetachProperty]
    public Base baseGeometry { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public FreeformElement() { }

    [SchemaInfo("Freeform element", "Creates a Revit Freeform element using a BREP or a Mesh.")]
    public FreeformElement(Brep baseGeometry, List<Parameter> parameters = null)
    {
      this.baseGeometry = baseGeometry;
      this.parameters = parameters;
    }
  }
}

using System;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Utils;

namespace Objects.BuiltElements.Revit
{
  public class FreeformElement : Base , IDisplayMesh
  {
    public Base parameters { get; set; }
    
    public string elementId { get; set; }

    [DetachProperty]
    public Base baseGeometry { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public string units { get; set; }

    public FreeformElement() { }

    [SchemaInfo("Freeform element", "Creates a Revit Freeform element using a Brep or a Mesh.", "Revit", "Families")]
    public FreeformElement([SchemaMainParam] Base baseGeometry, List<Parameter> parameters = null)
    {
      if (!IsValidObject(baseGeometry))
        throw new Exception("Freeform elements can only be created from BREPs or Meshes");
      this.baseGeometry = baseGeometry;
      this.parameters = parameters.ToBase();
    }
    
    public bool IsValid() => IsValidObject(baseGeometry);
    
    public bool IsValidObject(Base @base) =>
      @base is Mesh
      || @base is Brep;
    
    
  }
}

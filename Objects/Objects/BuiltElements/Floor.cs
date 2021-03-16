using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Floor : Base, IDisplayMesh
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [DetachProperty]
    public List<Base> elements { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public Floor() { }

    [SchemaInfo("Floor", "Creates a Speckle floor")]
    public Floor(ICurve outline, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have")] List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitFloor : Floor
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public bool structural { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }
    public RevitFloor() { }

    [SchemaInfo("RevitFloor", "Creates a Revit floor by outline and level")]
    public RevitFloor(string family, string type, ICurve outline,
       Level level, bool structural = false, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have")] List<Base> elements = null,
      List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.level = level;
      this.structural = structural;
      this.parameters = parameters;
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }
  }
}
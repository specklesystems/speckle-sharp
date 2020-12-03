using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Floor : Base
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    public List<Base> elements { get; set; }

    public Floor() { }

    [SchemaInfo("Floor", "Creates a Speckle floor")]
    public Floor(ICurve outline, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have.")] List<Base> elements = null)
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
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
    public RevitFloor()
    {
    }

    [SchemaInfo("RevitFloor", "Creates a Revit floor by outline and level")]
    public RevitFloor(ICurve outline, string family, string type,
       Level level, bool structural = false, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this floor might have.")] List<Base> elements = null,
      Dictionary<string, object> parameters = null)
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
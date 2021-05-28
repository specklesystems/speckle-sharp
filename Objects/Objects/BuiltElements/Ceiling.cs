using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements
{
  public class Ceiling : Base, IDisplayMesh
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    [DetachProperty]
    public List<Base> elements { get; set; }

    public Ceiling() { }

    [SchemaInfo("Ceiling", "Creates a Speckle ceiling")]
    public Ceiling([SchemaMainParam] ICurve outline, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this ceiling might have")] List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitCeiling : Ceiling
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public double offset { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }

    public RevitCeiling() { }
  }
}
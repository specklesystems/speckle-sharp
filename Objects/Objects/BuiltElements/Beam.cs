using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Beam : Base, IDisplayMesh
  {
    public ICurve baseLine { get; set; }

    [DetachProperty]
    public Mesh displayMesh { get; set; }

    public Beam() { }

    [SchemaInfo("Beam", "Creates a Speckle beam")]
    public Beam(ICurve baseLine)
    {
      this.baseLine = baseLine;
    }
  }
}

namespace Objects.BuiltElements.Revit
{
  public class RevitBeam : Beam
  {
    public string family { get; set; }
    public string type { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }
    public Level level { get; set; }

    public RevitBeam() { }

    [SchemaInfo("RevitBeam", "Creates a Revit beam by curve and base level.")]
    public RevitBeam(string family, string type, ICurve baseLine, Level level, List<Parameter> parameters = null)
    {
      this.family = family;
      this.type = type;
      this.baseLine = baseLine;
      this.parameters = parameters;
      this.level = level;
    }
  }
}

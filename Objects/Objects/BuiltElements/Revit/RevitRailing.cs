using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class RevitRailing : Base
  {
    //public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public Polycurve path { get; set; }
    public bool flipped { get; set; }

    public string elementId { get; set; }

    public List<Parameter> parameters { get; set; }


    public RevitRailing() { }

    [SchemaInfo("Railing", "Creates a Revit railing by base curve.")]
    public RevitRailing(string type, Polycurve baseCurve, Level level, bool flipped = false)
    {
      this.type = type;
      this.path = baseCurve;
      this.level = level;
      this.flipped = flipped;
    }


  }
}

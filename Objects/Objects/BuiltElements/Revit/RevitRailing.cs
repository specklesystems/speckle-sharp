using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Objects.BuiltElements.Revit
{
  public class RevitRailing : Base, IDisplayValue<List<Mesh>>
  {
    //public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public Polycurve path { get; set; }
    public bool flipped { get; set; }
    public string elementId { get; set; }
    public Base parameters { get; set; }
    public RevitTopRail topRail { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public RevitRailing() { }

    [SchemaInfo("Railing", "Creates a Revit railing by base curve.", "Revit", "Architecture")]
    public RevitRailing(string type, [SchemaMainParam] Polycurve baseCurve, Level level, bool flipped = false)
    {
      this.type = type;
      this.path = baseCurve;
      this.level = level;
      this.flipped = flipped;
    }
  }

  // Used only to transfer parameters of the top railing
  // its display mesh will live in the main railing element
  public class RevitTopRail : Base
  {
    //public string family { get; set; }
    public string type { get; set; }
    public string elementId { get; set; }
    public Base parameters { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public RevitTopRail() { }

  }
}

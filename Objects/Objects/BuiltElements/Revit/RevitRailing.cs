using System;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Revit
{
  public class RevitRailing : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
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

    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh
    {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> { value };
    }
    #endregion
  }
}

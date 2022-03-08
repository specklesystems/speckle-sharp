using System;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class BuiltElement2D : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public ICurve outline { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    [DetachProperty]
    public List<Base> elements { get; set; }

    public string units { get; set; }

    public BuiltElement2D() { }

    [SchemaInfo("BuiltElement2D", "Creates a Speckle BuiltElement2D", "BIM", "Architecture")]
    public BuiltElement2D([SchemaMainParam] ICurve outline, List<ICurve> voids = null,
      [SchemaParamInfo("Any nested elements that this BuiltElement2D might have")] List<Base> elements = null)
    {
      this.outline = outline;
      this.voids = voids;
      this.elements = elements;
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
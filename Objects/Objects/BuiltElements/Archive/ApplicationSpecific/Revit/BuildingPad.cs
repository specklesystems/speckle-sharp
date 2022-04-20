using System;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Revit
{
  public class BuildingPad : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public ICurve outline { get; set; }
    
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    
    public string type { get; set; }
    
    public Level level { get; set; }
    
    public Base parameters { get; set; }
    
    public string elementId { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public BuildingPad() { }
    
    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> {value};
    }
    #endregion
  }
}

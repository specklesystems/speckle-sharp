using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;
using Objects.Structural.Materials;

namespace Objects.BuiltElements
{
  public class Rebar : Base, IHasVolume, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public List<ICurve> curves { get; set; } = new List<ICurve>();
    
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    
    public string units { get; set; }
    public double volume { get; set; }

    public Rebar() { }
    
    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> {value};
    }
    #endregion
  }
}

namespace Objects.BuiltElements.TeklaStructures
{
  public class TeklaRebar : Rebar
  {
    public TeklaRebar()
    {
    }

    public string name { get; set; }
    
    [DetachProperty]
    public Hook startHook { get; set; }
    [DetachProperty]
    public Hook endHook { get; set; }
    public double classNumber { get; set; }
    public string size { get; set; }

    [DetachProperty]
    public StructuralMaterial material { get; set; }
  }
  public class Hook :Base {
    public Hook()
    {
    }

    public double angle { get; set; }
    public double length { get; set; }
    public double radius { get; set; }
    public shape shape { get; set; }
  }
  public enum shape
  {
    NO_HOOK = 0,
    HOOK_90_DEGREES = 1,
    HOOK_135_DEGREES = 2,
    HOOK_180_DEGREES = 3,
    CUSTOM_HOOK = 4
  }

}
namespace Objects.BuiltElements.Revit
{
  public class RevitRebar : Rebar
  {
    public string family { get; set; }
    public string type { get; set; }
    public string host { get; set; }
    public string barType { get; set; }
    public string barStyle { get; set; }
    public List<string> shapes { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }

    public RevitRebar() { }

  }
}

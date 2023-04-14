using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
  public class Rebar : Base, IHasVolume, IDisplayValue<List<Mesh>>
  {
    public List<ICurve> curves { get; set; } = new();

    public string units { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public double volume { get; set; }
  }
}

namespace Objects.BuiltElements.TeklaStructures
{
  public class TeklaRebar : Rebar
  {
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

  public class Hook : Base
  {
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
  }
}

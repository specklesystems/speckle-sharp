using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class BuildingPad : Base, IDisplayValue<List<Mesh>>
{
  public ICurve outline { get; set; }

  public List<ICurve> voids { get; set; } = new();

  public string type { get; set; }

  public Level level { get; set; }

  public Base parameters { get; set; }

  public string elementId { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}

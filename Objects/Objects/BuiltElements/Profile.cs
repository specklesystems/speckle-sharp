using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Profile : Base, IDisplayValue<Polyline>
{
  public List<ICurve> curves { get; set; }

  public string name { get; set; }

  public double startStation { get; set; }

  public double endStation { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public Polyline displayValue { get; set; }
}

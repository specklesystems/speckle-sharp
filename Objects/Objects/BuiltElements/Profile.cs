using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Profile : Base, IDisplayValue<Polyline>
  {
    public List<ICurve> curves { get; set; }

    public string name { get; set; }

    public double startStation { get; set; }

    public double endStation { get; set; }

    [DetachProperty]
    public Polyline displayValue { get; set; }

    public string units { get; set; }

    public Profile() { }

  }
}

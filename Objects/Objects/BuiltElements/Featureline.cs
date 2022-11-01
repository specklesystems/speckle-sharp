using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using Speckle.Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Featureline : Base
  {
    [JsonIgnore, Obsolete("Use curve property")]
    public ICurve baseCurve { get; set; }

    public ICurve curve { get; set; }

    public string name { get; set; }

    public string units { get; set; }

    [DetachProperty]
    public Polyline displayValue { get; set; }

    public Featureline() { }

  }
}

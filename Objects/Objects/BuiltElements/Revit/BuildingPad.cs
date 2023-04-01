using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements.Revit
{
  public class BuildingPad : Base, IDisplayValue<List<Mesh>>
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
  }
}

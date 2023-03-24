using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Area : Base, IHasArea, IHasVolume, IDisplayValue<List<Mesh>>
  {
    public string name { get; set; }
    public string number { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
    public Level level { get; set; }
    public Point center { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    public ICurve outline { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Area() { }

    /// <summary>
    /// SchemaBuilder constructor for a Room
    /// </summary>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
    [SchemaInfo("Area", "Creates a Speckle area", "BIM", "Other")]
    public Area(string name, string number, Level level, [SchemaMainParam] Point center)
    {
      this.name = name;
      this.number = number;
      this.level = level;
      this.center = center;
    }

  }
}

using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Room : Base, IHasArea, IHasVolume, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public string name { get; set; }
    public string number { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
    public Level level { get; set; }
    public Point basePoint { get; set; }
    public List<ICurve> voids { get; set; } = new List<ICurve>();
    public ICurve outline { get; set; }
    
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    
    public string units { get; set; }

    public Room() { }

    /// <summary>
    /// SchemaBuilder constructor for a Room
    /// </summary>
    /// <remarks>Assign units when using this constructor due to <paramref name="height"/> param</remarks>
    [SchemaInfo("Room", "Creates a Speckle room", "BIM", "Architecture")]
    public Room(string name, string number, Level level, [SchemaMainParam] Point basePoint)
    {
      this.name = name;
      this.number = number;
      this.level = level;
      this.basePoint = basePoint;
    }
    
    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> {value};
    }
    #endregion
  }
}

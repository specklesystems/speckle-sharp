using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

using Speckle.Core.Models;
using Objects.Geometry;

namespace Objects.BuiltElements.TeklaStructures
{
  public class Welds : Base
  {

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    public string mainObjectId { get; set; }
    public string secondaryObjectId { get; set; }
    public double sizeAbove { get; set; }
    public double sizeBelow { get; set; }
    public double lengthAbove { get; set; }
    public double lengthBelow { get; set; }
    public double pitchAbove { get; set; }
    public double pitchBelow { get; set; }
    public double angleAbove { get; set; } // In degrees
    public double angleBelow { get; set; } // In degrees
    public TeklaWeldType typeAbove { get; set; }
    public TeklaWeldType typeBelow { get; set; }
    public TeklaWeldIntermittentType intermittentType { get; set; }


    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh
    {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> { value };
    }
    #endregion
    public Welds() { }

  }
  public class PolygonWelds : Welds
  {
    public Polyline polyline { get; set; }
    public PolygonWelds() { }
  }
}

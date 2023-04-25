using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.TeklaStructures;

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
}

public class PolygonWelds : Welds
{
  public Polyline polyline { get; set; }
}

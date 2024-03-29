using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class GisFeature : Base
{
  public GisFeature()
  {
    geometry = new List<Base>();
    nativeGeometryType = "None";
  }

  [DetachProperty]
  public List<Base> geometry { get; set; }

  [DetachProperty]
  public List<Base>? displayValue { get; set; }
  public Base? attributes { get; set; }
  public string nativeGeometryType { get; set; }
  public string? speckleGeometryType { get; set; }
}

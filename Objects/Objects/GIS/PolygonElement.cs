using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class PolygonElement : Base
{
  [DetachProperty]
  public List<object> geometry { get; set; }
  public Base attributes { get; set; }
}

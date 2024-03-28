using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.GIS;

public class PolygonElement : Base
{
  public List<Base> geometry { get; set; }
  public Base attributes { get; set; }
}

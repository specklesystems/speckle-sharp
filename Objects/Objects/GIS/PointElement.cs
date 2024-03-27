using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.GIS;

public class PointElement : Base
{
  public PointElement(List<Point> list, Base atts)
  {
    geometry = list;
    attributes = atts;
  }

  public List<Point>? geometry { get; set; }
  public Base? attributes { get; set; }
}

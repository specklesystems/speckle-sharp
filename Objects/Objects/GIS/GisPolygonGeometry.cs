using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.GIS;

public class GisPolygonGeometry : Base
{
  public Polyline boundary { get; set; }
  public List<Polyline>? voids { get; set; }
}

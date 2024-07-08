using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.GIS;

public class PolygonGeometry : Base
{
  public string units { get; set; }
  public Polyline boundary { get; set; }
  public List<Polyline> voids { get; set; }

  public PolygonGeometry()
  {
    voids = new List<Polyline>();
  }
}

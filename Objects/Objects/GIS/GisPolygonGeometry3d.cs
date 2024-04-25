using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.GIS;

public class GisPolygonGeometry3d : Base
{
  public string units { get; set; }
  public Polyline boundary { get; set; }
  public List<Polyline> voids { get; set; }

  public GisPolygonGeometry3d()
  {
    voids = new List<Polyline>();
    units = Units.Meters;
  }
}

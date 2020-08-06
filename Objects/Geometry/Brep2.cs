using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Geometry
{
  public class Brep2 : Base, IGeometry
  {
    public List<Point> vertices { get; set; }
    public List<object> surfaces { get; set; }
    public List<ICurve> edges { get; set; }
  }
}

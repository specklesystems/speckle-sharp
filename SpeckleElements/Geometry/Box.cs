using Speckle.Elements.Simple;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Box : Base
  {
    public Plane basePlane { get; set; }
    public Interval xSize { get; set; }
    public Interval ySize { get; set; }
    public Interval zSize { get; set; }

    public Box()
    {

    }

    public Box(Plane basePlane, Interval xSize, Interval ySize, Interval zSize, string applicationId = null)
    {
      this.basePlane = basePlane;
      this.xSize = xSize;
      this.ySize = ySize;
      this.zSize = zSize;
      this.applicationId = applicationId;
    }
  }
}

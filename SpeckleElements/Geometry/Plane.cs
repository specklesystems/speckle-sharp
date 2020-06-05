using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Plane : Base
  {
    public Point origin { get; set; }
    public Vector normal { get; set; }
    public Vector xdir { get; set; }
    public Vector ydir { get; set; }

    public Plane()
    {

    }
    public Plane(Point origin, Vector normal, 
      Vector xDir, Vector yDir, string applicationId = null)
    {
      this.origin = origin;
      this.normal = normal;
      this.xdir = xDir;
      this.ydir = yDir;
      this.applicationId = applicationId;
    }
  }
}

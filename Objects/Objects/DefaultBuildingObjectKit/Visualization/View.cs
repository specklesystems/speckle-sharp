using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.DefaultBuildingObjectKit.Visualization
{


  public class View : Base
  {
    public string name { get; set; }

    public View() { }
  }

  public class View3D : View
  {
    public Point origin { get; set; }
    public Point target { get; set; }
    public Vector normalDirection { get; set; }
    public Vector forwardDirection { get; set; }

    public Box boundingBox { get; set; } // x is right, y is top of screen, z is towards viewer
    public bool isOrthogonal { get; set; } = true; // best to use an orthogonal view by default ?? - rey

    public string units { get; set; }

    public View3D() { }
  }

  public class View2D : View
  {
    public Point topLeft { get; set; }
    public Point bottomRight { get; set; }

    public View2D() { }
  }
}

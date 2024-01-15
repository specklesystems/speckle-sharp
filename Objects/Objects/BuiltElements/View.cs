using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class View : Base
{
  public string name { get; set; }
}

public class View3D : View
{
  public Point origin { get; set; }
  public Point target { get; set; }
  public Vector upDirection { get; set; }
  public Vector forwardDirection { get; set; }
  public Box boundingBox { get; set; } // x is right, y is top of screen, z is towards viewer
  public bool isOrthogonal { get; set; }

  public string units { get; set; }
}

public class View2D : View
{
  //public Point topLeft { get; set; }
  //public Point bottomRight { get; set; }
}

using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

public class Station : Base
{
  public double number { get; set; }
  public string type { get; set; }
  public Point location { get; set; }

  public string units { get; set; }
}

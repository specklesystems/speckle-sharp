using Objects.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Duct : IDuct
  {
    public double width { get; set; }
    public double height { get; set; }
    public double diameter { get; set; }
    public double length { get; set; }
    public double velocity { get; set; }

    public Line baseLine { get; set; }

    public Duct() { }
  }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects
{
  public class Duct : Element
  {
    public double width { get; set; }
    public double height { get; set; }
    public double diameter { get; set; }
    public double length { get; set; }
    public Level refLevel { get; set; }
    public double velocity { get; set; }

    public Duct() { }
  }
}

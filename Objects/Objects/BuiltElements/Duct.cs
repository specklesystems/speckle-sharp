using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Duct : Element
  {
    public double width { get; set; }
    public double height { get; set; }
    public double diameter { get; set; }
    public double length { get; set; }
    public Level level { get; set; }
    public double velocity { get; set; }
    public string system { get; set; }

    public Duct() { }
  }
}

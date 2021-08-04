using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Objects.BuiltElements
{
  public class Station : Base
  {
    public double number { get; set; }
    public string type { get; set; }
    public Point location { get; set; }

    public string units { get; set; }

    public Station() { }
  }
}

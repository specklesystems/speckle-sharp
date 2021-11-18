using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Objects.Other
{
  /// <summary>
  /// Hatch class for Rhino and AutoCAD
  /// </summary>
  public class Hatch : Base
  {
    public List<ICurve> curves { get; set; }
    public string pattern { get; set; }
    public double scale { get; set; } = 1;
    public double rotation { get; set; } = 0; // relative angle

    public Hatch() { }
  }
}

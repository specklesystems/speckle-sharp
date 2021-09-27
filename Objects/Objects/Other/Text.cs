using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Point = Objects.Geometry.Point;

namespace Objects.Other
{
  /// <summary>
  /// Text class for Rhino and AutoCAD
  /// </summary>
  public class Text : Base
  {
    public List<ICurve> curves { get; set; }
    public Point position { get; set; }
    public double rotation { get; set; } = 0; // using degrees
    public string font { get; set; }
    public string value { get; set; } // rich text formatting to include multiple lines
    public string alignment { get; set; }
    public double height { get; set; }
    public string units { get; set; }

    public Text() { }
  }
}

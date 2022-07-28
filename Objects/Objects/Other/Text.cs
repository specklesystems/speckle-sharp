using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Plane = Objects.Geometry.Plane;
using Polyline = Objects.Geometry.Polyline;

namespace Objects.Other
{
  /// <summary>
  /// Text class for Rhino and AutoCAD
  /// </summary>
  public class Text : Base, IDisplayValue<List<Polyline>>
  {
    public List<Polyline> displayValue { get; set; } = new List<Polyline>();
    public Plane plane { get; set; } // origin should be center
    public double rotation { get; set; } = 0; // using radians
    public string value { get; set; } // text without RTF
    public string richText { get; set; }
    public double height { get; set; }
    public string units { get; set; }

    public Text() { }
  }
}

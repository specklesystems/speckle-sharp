using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Objects.Other
{
  /// <summary>
  /// Minimal display style class. Developed primarily for display styles in Rhino and AutoCAD.
  /// Rhino object attributes uses OpenNURBS definition for linetypes and lineweights
  /// </summary>
  public class DisplayStyle : Base
  {
    public string name { get; set; }
    public int color { get; set; } = Color.LightGray.ToArgb(); // opacity assumed from a value
    public string linetype { get; set; }
    public double lineweight { get; set; } // assumed in mm
    public string units { get; set; }

    public DisplayStyle() { }
  }
}

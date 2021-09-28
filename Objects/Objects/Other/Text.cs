using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Plane = Objects.Geometry.Plane;

namespace Objects.Other
{
  /// <summary>
  /// Text class for Rhino and AutoCAD
  /// </summary>
  public class Text : Base
  {
    public List<ICurve> curves { get; set; }
    public Plane plane { get; set; } // origin is position
    public double rotation { get; set; } = 0; // using degrees
    public string value { get; set; } // text without RTF
    public string richText { get; set; }
    public string horizontalAlignment { get; set; }
    public string verticalAlignment { get; set; }
    public double height { get; set; }
    public string units { get; set; }

    public Text() { }
  }
}

using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Other;

/// <summary>
/// Text class for Rhino and AutoCAD
/// </summary>
public class Text : Base, IDisplayValue<List<Polyline>>
{
  public Plane plane { get; set; } // origin should be center
  public double rotation { get; set; } = 0; // using radians
  public string value { get; set; } // text without RTF
  public string richText { get; set; }
  public double height { get; set; }
  public string units { get; set; }
  public List<Polyline> displayValue { get; set; } = new();
}

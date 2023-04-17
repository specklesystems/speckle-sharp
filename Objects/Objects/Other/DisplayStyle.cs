using System.Drawing;
using Speckle.Core.Models;

namespace Objects.Other;

/// <summary>
/// Minimal display style class. Developed primarily for display styles in Rhino and AutoCAD.
/// Rhino object attributes uses OpenNURBS definition for linetypes and lineweights
/// </summary>
public class DisplayStyle : Base
{
  public string name { get; set; }
  public int color { get; set; } = Color.LightGray.ToArgb(); // opacity assumed from a value
  public string linetype { get; set; }

  /// <summary>
  /// The plot weight in the style units
  /// </summary>
  /// <remarks>A value of 0 indicates a default weight, and -1 indicates an invisible line</remarks>
  public double lineweight { get; set; }

  public string units { get; set; }
}

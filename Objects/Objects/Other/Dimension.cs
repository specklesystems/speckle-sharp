using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Vector = Objects.Geometry.Vector;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;

namespace Objects.Other
{
  /// <summary>
  /// Dimension class 
  /// </summary>
  public class Dimension : Base, IDisplayValue<List<ICurve>>
  {
    /// <summary>
    /// The measurement of the dimension.
    /// </summary>
    public double measurement { get; set; }

    /// <summary>
    /// The text of the dimension.
    /// </summary>
    public string text { get; set; }

    /// <summary>
    /// The position of the dimension
    /// </summary>
    public Point position { get; set; }

    /// <summary>
    /// Curves representing the annotation 
    /// </summary>
    public List<ICurve> displayValue { get; set; } = new List<ICurve>();

    public string units { get; set; }

    public Dimension() { }
  }

  /// <summary>
  /// Dimension class measuring a distance
  /// </summary>
  public class DistanceDimension : Dimension
  {
    /// <summary>
    /// The unitized normal of the dimension.
    /// </summary>
    public Vector direction { get; set; }

    /// <summary>
    /// The objects being measured.
    /// </summary>
    /// <remarks>
    /// Distance measurements are between two points
    /// </remarks>
    public List<Point> measured { get; set; }

    public DistanceDimension() { }
  }

  /// <summary>
  /// Dimension class measuring a length
  /// </summary>
  public class LengthDimension : Dimension
  {
    /// <summary>
    /// The objects being measured.
    /// </summary>
    /// <remarks>
    /// For length dimensions, this is a curve
    /// </remarks>
    public ICurve measured { get; set; }

    public LengthDimension() { }
  }

  /// <summary>
  /// Dimension class measuring an angle
  /// </summary>
  public class AngleDimension : Dimension
  {
    /// <summary>
    /// The objects being measured.
    /// </summary>
    /// <remarks>
    /// For angle dimensions, these are representated as two lines.
    /// </remarks>
    public List<Line> measured { get; set; }

    public AngleDimension() { }
  }
}

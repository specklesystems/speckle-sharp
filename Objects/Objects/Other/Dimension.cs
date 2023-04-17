using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Other;

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
  /// The text of the dimension, without any formatting
  /// </summary>
  public string value { get; set; }

  /// <summary>
  /// The text of the dimension, with rtf formatting
  /// </summary>
  public string richText { get; set; }

  /// <summary>
  /// The position of the dimension
  /// </summary>
  public Point position { get; set; }

  /// <summary>
  /// The position of the text of the dimension
  /// </summary>
  public Point textPosition { get; set; }

  public string units { get; set; }

  /// <summary>
  /// Curves representing the annotation
  /// </summary>
  public List<ICurve> displayValue { get; set; } = new();
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
  /// Indicates if this dimension is an ordinate dimension
  /// </summary>
  /// <remarks> Ordinate dimensions (measuring distance between two points exclusively along the x or y axis)
  /// are in practice drawn with different conventions than linear dimensions, and are treated as a special subset of them.</remarks>
  public bool isOrdinate { get; set; }

  /// <summary>
  /// The objects being measured.
  /// </summary>
  /// <remarks>
  /// Distance measurements are between two points
  /// </remarks>
  public List<Point> measured { get; set; }
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
  /// For angle dimensions, this is two lines
  /// </remarks>
  public List<Line> measured { get; set; }
}

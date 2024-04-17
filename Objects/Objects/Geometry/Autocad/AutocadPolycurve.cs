using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.Geometry.Autocad;

/// <summary>
/// A curve that is comprised of line, arc and/or curve segments, representing the Autocad Polyline, Polyline2d, and Polyline3d classes.
/// </summary>
/// <remarks>
/// <see cref="AutocadPolyType.Light"/> will have only <see cref="Line"/>s and <see cref="Arc"/>s in <see cref="Polycurve.segments"/>.
/// <see cref="AutocadPolyType.Simple3d"/> type will have only <see cref="Line"/>s in <see cref="Polycurve.segments"/>.
/// <see cref="AutocadPolyType.FitCurve2d"/> type will only have <see cref="Arc"/>s in <see cref="Polycurve.segments"/>.
/// <see cref="AutocadPolyType.CubicSpline2d"/>, <see cref="AutocadPolyType.CubicSpline3d"/>, <see cref="AutocadPolyType.QuadSpline2d"/>, and <see cref="AutocadPolyType.QuadSpline3d"/> types will have only a single <see cref="Curve"/>s in <see cref="Polycurve.segments"/>.
/// </remarks>
public class AutocadPolycurve : Polycurve
{
  /// <summary>
  /// Constructs a new empty <see cref="AutocadPolycurve"/> instance.
  /// </summary>
  public AutocadPolycurve() { }

  /// <summary>
  /// Gets or sets the raw coordinates of the vertices.
  /// </summary>
  /// <remarks>
  /// For <see cref="AutocadPolyType.Light"/> Polylines, these are xy coordinates in the Object Coordinate System (OCS) of the <see cref="plane"/>.
  /// For Polyline2d and Polyline3d types, these are xyz coordinates in the Global Coordinate System. fml.
  /// </remarks>
  [DetachProperty, Chunkable(31250)]
  public List<double> value { get; set; } = new();

  /// <summary>
  /// The bulge factor at each vertex. Should be null for Polyline3d.
  /// </summary>
  /// <remarks>
  /// The bulge factor is used to indicate how much of an arc segment is present at this vertex.
  /// The bulge factor is the tangent of one fourth the included angle for an arc segment,
  /// made negative if the arc goes clockwise from the start point to the endpoint.
  /// A bulge of 0 indicates a straight segment, and a bulge of 1 is a semicircle.
  /// </remarks>
  public List<double>? bulges { get; set; }

  /// <summary>
  /// The tangent in radians at each vertex. Should be null for Polyline and Polyline3d.
  /// </summary>
  public List<double>? tangents { get; set; }

  /// <summary>
  /// The plane of the Autocad Polyline or Polyline2d. Should be null for Polyline3d.
  /// </summary>
  /// <remarks></remarks>
  public Plane? plane { get; set; }

  public AutocadPolyType polyType { get; set; }
}

/// <summary>
/// Represents the type of a Autocad Polyline.
/// </summary>
public enum AutocadPolyType
{
  /// Polyline type is not known
  Unknown,

  /// Polyline type is the Autocad Polyline class
  Light,

  Simple3d,

  /// The Autocad Polyline2d fit curve poly type. Constructed with pairs of arcs with continuous tangents.
  FitCurve2d,

  CubicSpline2d,

  CubicSpline3d,

  QuadSpline2d,

  QuadSpline3d,
}

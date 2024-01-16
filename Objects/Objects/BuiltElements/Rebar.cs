using System;
using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements;

/// <summary>
/// A reinforcement bar group comprised of reinforcing bars of the same type and shape.
/// </summary>
/// <remarks>
/// This class is not suitable for freeform rebar, which can have multiple shapes.
/// </remarks>
public class RebarGroup<T> : Base, IHasVolume, IDisplayValue<List<ICurve>>
  where T : RebarShape
{
  public RebarGroup() { }

  /// <summary>
  /// The shape of the rebar group
  /// </summary>
  [DetachProperty]
  public RebarShape shape { get; set; }

  /// <summary>
  /// The number of rebars in the rebar group
  /// </summary>
  /// <remarks>
  /// Excluded end bars are not included in the count
  /// </remarks>
  public int number { get; set; }

  /// <summary>
  /// Indicates if rebar set includes the first bar
  /// </summary>
  /// <remarks>
  /// Only applicable to stirrup (transverse) rebar
  /// </remarks>
  public bool hasFirstBar { get; set; }

  /// <summary>
  /// Indicates if rebar set includes the last bar
  /// </summary>
  /// <remarks>
  /// Only applicable to stirrup (transverse) rebar
  /// </remarks>
  public bool hasLastBar { get; set; }

  /// <summary>
  /// The start hook of bars in the rebar group
  /// </summary>
  /// <remarks>
  /// Null indicates no start hook
  /// </remarks>
  [DetachProperty]
  public virtual RebarHook? startHook { get; set; }

  /// <summary>
  /// The end hook of bars in the rebar group
  /// </summary>
  /// <remarks>
  /// Null indicates no end hook
  /// </remarks>
  [DetachProperty]
  public virtual RebarHook? endHook { get; set; }

  /// <summary>
  /// The display representation of the rebar group as centerline curves
  /// </summary>
  [DetachProperty]
  public List<ICurve> displayValue { get; set; }

  /// <summary>
  /// The total volume of the rebar group.
  /// </summary>
  public double volume { get; set; }

  public string units { get; set; }
}

/// <summary>
/// The shape describing the geometry and geometry parameters of a reinforcing bar
/// </summary>
public class RebarShape : Base
{
  public RebarShape() { }

  /// <summary>
  /// The name of the rebar shape
  /// </summary>
  public string name { get; set; }

  /// <summary>
  /// The type of the rebar shape
  /// </summary>
  public RebarType rebarType { get; set; }

  /// <summary>
  /// The curves of the rebar shape
  /// </summary>
  /// <remarks>
  /// Typically suppresses hooks and bend radius
  /// </remarks>
  public List<ICurve> curves { get; set; } = new();

  /// <summary>
  /// The diameter of the rebar bar
  /// </summary>
  public double barDiameter { get; set; }

  public string units { get; set; }
}

public class RebarHook : Base
{
  public RebarHook() { }

  /// <summary>
  /// The angle of the hook in radians.
  /// </summary>
  public double angle { get; set; }

  /// <summary>
  /// The length of the hook.
  /// </summary>
  public double length { get; set; }

  /// <summary>
  /// The radius of the bend of the hook.
  /// </summary>
  public double radius { get; set; }

  public string units { get; set; }
}

public enum RebarType
{
  Unknown = 0,
  Standard = 10,
  StirrupPolygonal = 20,
  StirrupSpiral = 30,
  StirrupTapered = 40
}

#region Obsolete
[Obsolete("Deprecated in 2.17: Use the RebarGroup class instead")]
public class Rebar : Base, IHasVolume, IDisplayValue<List<Mesh>>
{
  public List<ICurve> curves { get; set; } = new();

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public double volume { get; set; }
}
#endregion

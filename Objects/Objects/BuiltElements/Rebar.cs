using System;
using System.Collections.Generic;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.TeklaStructures;
using Objects.Geometry;
using Objects.Structural.Materials;
using Speckle.Core.Models;

namespace Objects.BuiltElements
{
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
}

namespace Objects.BuiltElements.TeklaStructures
{
  #region Obsolete
  [Obsolete("Deprecated in 2.17: Create a TeklaRebarGroup class instead")]
  public class TeklaRebar : Rebar
  {
    public string name { get; set; }

    [DetachProperty]
    public Hook startHook { get; set; }

    [DetachProperty]
    public Hook endHook { get; set; }

    public double classNumber { get; set; }
    public string size { get; set; }

    [DetachProperty]
    public StructuralMaterial material { get; set; }
  }

  [Obsolete("Deprecated in 2.17: Use a RebarHook class instead")]
  public class Hook : Base
  {
    public double angle { get; set; }
    public double length { get; set; }
    public double radius { get; set; }
    public shape shape { get; set; }
  }

  [Obsolete("Deprecated in 2.17: set starthook and endhook to null or refer to hook angle instead")]
  public enum shape
  {
    NO_HOOK = 0,
    HOOK_90_DEGREES = 1,
    HOOK_135_DEGREES = 2,
    HOOK_180_DEGREES = 3,
    CUSTOM_HOOK = 4
  }
  #endregion
}

namespace Objects.BuiltElements.Revit
{
  public class RevitRebarGroup : RebarGroup<RevitRebarShape>
  {
    public RevitRebarGroup() { }

    [JsonIgnore]
    public RevitRebarShape revitShape { get; set; }

    public override RebarHook? startHook
    {
      get => revitStartHook;
      set
      {
        if (value is not RevitRebarHook && value is not null)
        {
          throw new ArgumentException($"Expected object of type {nameof(RevitRebarHook)} or null");
        }

        revitStartHook = (RevitRebarHook)value;
      }
    }

    [JsonIgnore]
    public RevitRebarHook? revitStartHook { get; set; }

    public override RebarHook? endHook
    {
      get => revitEndHook;
      set
      {
        if (value is not RevitRebarHook && value is not null)
        {
          throw new ArgumentException($"Expected object of type {nameof(RevitRebarHook)} or null");
        }

        revitEndHook = (RevitRebarHook)value;
      }
    }

    [JsonIgnore]
    public RevitRebarHook? revitEndHook { get; set; }

    public string family { get; set; }
    public string type { get; set; }
    public int barPositions { get; set; }
    public Vector normal { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
  }

  public class RevitRebarShape : RebarShape
  {
    public RevitRebarShape() { }

    public Base parameters { get; set; }
    public string elementId { get; set; }
  }

  public class RevitRebarHook : RebarHook
  {
    public RevitRebarHook() { }

    public double multiplier { get; set; }
    public string orientation { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
  }

  #region Obsolete
  [Obsolete("Deprecated in 2.17: Use RevitRebarGroup class instead", true)]
  public class RevitRebar : Rebar
  {
    public string family { get; set; }
    public string type { get; set; }
    public string host { get; set; }
    public string barType { get; set; }
    public string barStyle { get; set; }
    public List<string> shapes { get; set; }
    public Base parameters { get; set; }
    public string elementId { get; set; }
  }
  #endregion
}

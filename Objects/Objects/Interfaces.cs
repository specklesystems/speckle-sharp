using System;
using Objects.Other;
using Objects.Geometry;
using Objects.Primitive;
using Objects.BuiltElements;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Properties;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Objects
{
  #region Generic interfaces.

  public interface IHasBoundingBox
  {
    Box bbox { get; }
  }

  public interface IHasArea
  {
    double area { get; set; }
  }

  public interface IHasVolume
  {
    double volume { get; set; }
  }

  public interface ICurve
  {
    double length { get; set; }
    Interval domain { get; set; }
    string units { get; set; }
  }

  /// <summary>
  /// Interface for transformable objects.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public interface ITransformable<T> where T : ITransformable<T>
  {
    bool TransformTo(Transform transform, out T transformed);
  }

  /// <summary>
  /// Interface for transformable objects where the type may not be known on convert (eg ICurve implementations)
  /// </summary>
  public interface ITransformable
  {
    bool TransformTo(Transform transform, out ITransformable transformed);
  }

  #endregion

  #region Built elements

  [Obsolete("Use " + nameof(IDisplayValue<Mesh>) + " instead")]
  public interface IDisplayMesh
  {
    [Obsolete("Use " + nameof(IDisplayValue<Mesh>) + "." + nameof(IDisplayValue<Mesh>.displayValue) + " instead")]
    Mesh displayMesh { get; set; }
  }

  /// <summary>
  /// Specifies displayable <see cref="Base"/> value(s) to be used as a fallback
  /// if a displayable form cannot be converted.
  /// </summary>
  /// <example>
  /// <see cref="Base"/> objects that represent conceptual / abstract / mathematically derived geometry
  /// can use <see cref="displayValue"/> to be used in case the object lacks a natively displayable form.
  /// (e.g <see cref="Spiral"/>, <see cref="Wall"/>, <see cref="Pipe"/>)
  /// </example>
  /// <typeparam name="T">
  /// Type of display value.
  /// Expected to be either a <see cref="Base"/> type or a <see cref="List{T}"/> of <see cref="Base"/>s,
  /// most likely <see cref="Mesh"/> or <see cref="Polyline"/>.
  /// </typeparam>
  public interface IDisplayValue<T>
  {
    /// <summary>
    /// <see cref="displayValue"/> <see cref="Base"/>(s) will be used to display this <see cref="Base"/>
    /// if a native displayable object cannot be converted.
    /// </summary>
    T displayValue { get; set; }
  }

  /// <summary>
  /// Basic interface to store application-specific properties
  /// </summary>
  /// <remarks>
  /// </remarks>
  public interface IHasSourceAppProps
  {
    /// <summary>
    /// Stores application-specific properties from the authoring application for higher fidelity intra-application data transfer.
    /// </summary>
    ApplicationProperties sourceApp { get; set; }
  }

  public interface IHasChildElements
  {
    /// <summary>
    /// Child elements of any <see cref="Base"/> type can be placed within the <see cref="elements"/> array (eg windows within a wall)
    /// </summary>
    List<Base> elements { get; set; }
  }

  /// <summary>
  /// Base class for all physical elements. This ensures they have a detached <see cref="displayValue"/> for visualisation
  /// when they can't be converted to native and a <see cref="sourceApp"/> property for storing properties from the
  /// authoring application.
  /// </summary>
  public abstract class PhysicalElement : Base, IDisplayValue<List<Mesh>>, IHasSourceAppProps
  {
    [DetachProperty] public List<Mesh> displayValue { get; set; }
    [DetachProperty] public ApplicationProperties sourceApp { get; set; }

    /// <summary>
    /// A string representing the abbreviated units (eg "m", "mm", "ft").
    /// Use the <see cref="Units"/> helper to ensure you're using the correct strings.
    /// </summary>
    public abstract string units { get; }
  }

  /// <summary>
  /// Base class for all <see cref="PhysicalElement"/>s that are defined by a <see cref="Point"/>
  /// </summary>
  public abstract class PointBasedElement : PhysicalElement
  {
    public Point basePoint { get; set; }
    
    /// <summary>
    /// A string representing the units of this element. It is defined by the units of the <see cref="basePoint"/>
    /// </summary>
    public override string units => basePoint?.units;
  }

  /// <summary>
  /// Base class for all <see cref="PhysicalElement"/>s that are defined by an <see cref="ICurve"/> <see cref="baseCurve"/>
  /// </summary>
  public abstract class CurveBasedElement : PhysicalElement, IHasChildElements
  {
    public ICurve baseCurve { get; set; }
    [DetachProperty] public List<Base> elements { get; set; }

    /// <summary>
    /// A string representing the units of this element. It is defined by the units of the <see cref="baseCurve"/>
    /// </summary>
    public override string units => baseCurve?.units;
  }
  
  /// <summary>
  /// Base class for all <see cref="PhysicalElement"/>s that are defined by an <see cref="ICurve"/> <see cref="outline"/>
  /// </summary>
  public abstract class OutlineBasedElement : PhysicalElement,  IHasChildElements
  {
    public ICurve outline { get; set; }
    [DetachProperty] public List<Base> elements { get; set; }

    /// <summary>
    /// A string representing the units of this element. It is defined by the units of the <see cref="outline"/>
    /// </summary>
    public override string units => outline?.units;
  }

  #endregion
}
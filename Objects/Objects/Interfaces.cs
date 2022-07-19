using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Primitive;
using Speckle.Core.Logging;

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
  }

  /// <summary>
  /// Interface for transformable objects.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public interface ITransformable<T>: ITransformable where T : ITransformable<T>
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
  

  #endregion
}

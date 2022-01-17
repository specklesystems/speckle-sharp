using Objects.BuiltElements;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Objects.Primitive;

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

  [Obsolete("Use " + nameof(IDisplayValues<Mesh>) + " instead")]
  public interface IDisplayMesh
  {
    [Obsolete("Use " + nameof(IDisplayValues<Mesh>) + "." + nameof(IDisplayValues<Mesh>.displayValues) + " instead")]
    Mesh displayMesh { get; set; }
  }
  
  /// <summary>
  /// Interface for fallback display values
  /// </summary>
  /// <typeparam name="TBase"></typeparam>
  public interface IDisplayValues<TBase> : IDisplayValues where TBase : Base
  {
    /// <summary>
    /// If no direct conversion exists for this object, these fallback <see cref="TBase"/>s will be used instead
    /// </summary>
    new List<TBase> displayValues { get; set; }
  }
  
  /// <summary>
  /// Non-generic version of <see cref="IDisplayValues{TBase}"/>.
  /// </summary>
  /// <remarks>
  /// Do not implement this interface directly, instead use <see cref="IDisplayValues{TBase}"/>.
  /// </remarks>
  public interface IDisplayValues
  {
    IReadOnlyList<Base> displayValues { get; }
  }

  #endregion
}

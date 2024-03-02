using Speckle.Core.Models;
using Speckle.Objects.Primitives;
using Speckle.Objects.ThreeD;

namespace Speckle.Objects.Geometry;

/// <summary>
/// Represents an object that has a <see cref="bbox"/>
/// </summary>
public interface IHasBoundingBox
{
  /// <summary>
  /// The bounding box containing the object.
  /// </summary>
  Box bbox { get; }
}

/// <summary>
/// Represents a <see cref="Base"/> object that has <see cref="area"/>
/// </summary>
public interface IHasArea
{
  /// <summary>
  /// The area of the object
  /// </summary>
  double area { get; set; }
}

/// <summary>
/// Represents an object that has <see cref="volume"/>
/// </summary>
public interface IHasVolume
{
  /// <summary>
  /// The volume of the object
  /// </summary>
  double volume { get; set; }
}

/// <summary>
/// Represents
/// </summary>
public interface ICurve
{
  /// <summary>
  /// The length of the curve.
  /// </summary>
  double length { get; set; }

  /// <summary>
  /// The numerical domain driving the curve's internal parametrization.
  /// </summary>
  Interval domain { get; set; }
}

/// <summary>
/// Generic Interface for transformable objects.
/// </summary>
/// <typeparam name="T">The type of object to support transformations.</typeparam>
public interface ITransformable<T> : ITransformable
  where T : ITransformable<T>
{
  /// <inheritdoc cref="ITransformable.TransformTo"/>
  bool TransformTo(Transform transform, out T transformed);
}

/// <summary>
/// Interface for transformable objects where the type may not be known on convert (eg ICurve implementations)
/// </summary>
public interface ITransformable
{
  /// <summary>
  /// Returns a copy of the object with it's coordinates transformed by the provided <paramref name="transform"/>
  /// </summary>
  /// <param name="transform">The <see cref="Transform"/> to be applied.</param>
  /// <param name="transformed">The transformed copy of the object.</param>
  /// <returns>True if the transform operation was successful, false otherwise.</returns>
  bool TransformTo(Transform transform, out ITransformable transformed);
}

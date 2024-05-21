using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PointToHostConverter : ITypedConverter<SOG.Point, RG.Point3d>, ITypedConverter<SOG.Point, RG.Point>
{
  /// <summary>
  /// Converts a Speckle Point object to a Rhino Point3d object.
  /// </summary>
  /// <param name="target">The Speckle Point object to convert.</param>
  /// <returns>The converted Rhino Point3d object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.Point3d Convert(SOG.Point target) => new(target.x, target.y, target.z);

  /// <summary>
  /// Converts a Speckle Point object to a Rhino Point object.
  /// </summary>
  /// <param name="target">The Speckle Point object to convert.</param>
  /// <returns>The converted Rhino Point object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  RG.Point ITypedConverter<SOG.Point, RG.Point>.Convert(SOG.Point target) => new(Convert(target));
}

using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class VectorToHostConverter : IRawConversion<SOG.Vector, RG.Vector3d>
{
  /// <summary>
  /// Converts a Speckle.Vector object to a Rhino Vector3d object.
  /// </summary>
  /// <param name="target">The Speckle.Vector to be converted.</param>
  /// <returns>The converted Rhino Vector3d object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.Vector3d RawConvert(SOG.Vector target) => new(target.x, target.y, target.z);
}

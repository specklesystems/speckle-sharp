using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class VectorToHostConverter : ITypedConverter<SOG.Vector, IRhinoVector3d>
{
  private readonly IRhinoVectorFactory _rhinoVectorFactory;

  public VectorToHostConverter(IRhinoVectorFactory rhinoVectorFactory)
  {
    _rhinoVectorFactory = rhinoVectorFactory;
  }

  /// <summary>
  /// Converts a Speckle.Vector object to a Rhino Vector3d object.
  /// </summary>
  /// <param name="target">The Speckle.Vector to be converted.</param>
  /// <returns>The converted Rhino Vector3d object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoVector3d Convert(SOG.Vector target) => _rhinoVectorFactory.Create(target.x, target.y, target.z);
}

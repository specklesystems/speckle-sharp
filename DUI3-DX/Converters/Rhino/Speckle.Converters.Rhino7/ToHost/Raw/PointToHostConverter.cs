using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PointToHostConverter : ITypedConverter<SOG.Point, IRhinoPoint3d>, ITypedConverter<SOG.Point, IRhinoPoint>
{
  private readonly IRhinoPointFactory _rhinoPointFactory;

  public PointToHostConverter(IRhinoPointFactory rhinoPointFactory)
  {
    _rhinoPointFactory = rhinoPointFactory;
  }

  /// <summary>
  /// Converts a Speckle Point object to a Rhino Point3d object.
  /// </summary>
  /// <param name="target">The Speckle Point object to convert.</param>
  /// <returns>The converted Rhino Point3d object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoPoint3d Convert(SOG.Point target) => _rhinoPointFactory.Create(target.x, target.y, target.z);

  /// <summary>
  /// Converts a Speckle Point object to a Rhino Point object.
  /// </summary>
  /// <param name="target">The Speckle Point object to convert.</param>
  /// <returns>The converted Rhino Point object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  IRhinoPoint ITypedConverter<SOG.Point, IRhinoPoint>.Convert(SOG.Point target) =>
    _rhinoPointFactory.Create(Convert(target));
}

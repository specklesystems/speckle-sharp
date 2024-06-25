using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PlaneToHostConverter : ITypedConverter<SOG.Plane, IRhinoPlane>
{
  private readonly ITypedConverter<SOG.Point, IRhinoPoint3d> _pointConverter;
  private readonly ITypedConverter<SOG.Vector, IRhinoVector3d> _vectorConverter;
  private readonly IRhinoPlaneFactory _rhinoPlaneFactory;

  public PlaneToHostConverter(
    ITypedConverter<SOG.Point, IRhinoPoint3d> pointConverter,
    ITypedConverter<SOG.Vector, IRhinoVector3d> vectorConverter,
    IRhinoPlaneFactory rhinoPlaneFactory
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
    _rhinoPlaneFactory = rhinoPlaneFactory;
  }

  /// <summary>
  /// Converts a Speckle Plane object to a Rhino Plane object.
  /// </summary>
  /// <param name="target">The Speckle Plane object to be converted.</param>
  /// <returns>The converted Rhino Plane object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoPlane Convert(SOG.Plane target) =>
    _rhinoPlaneFactory.Create(
      _pointConverter.Convert(target.origin),
      _vectorConverter.Convert(target.xdir),
      _vectorConverter.Convert(target.ydir)
    );
}

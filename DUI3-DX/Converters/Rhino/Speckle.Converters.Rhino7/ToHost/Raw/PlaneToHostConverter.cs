using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PlaneToHostConverter : ITypedConverter<SOG.Plane, RG.Plane>
{
  private readonly ITypedConverter<SOG.Point, RG.Point3d> _pointConverter;
  private readonly ITypedConverter<SOG.Vector, RG.Vector3d> _vectorConverter;

  public PlaneToHostConverter(
    ITypedConverter<SOG.Point, RG.Point3d> pointConverter,
    ITypedConverter<SOG.Vector, RG.Vector3d> vectorConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  /// <summary>
  /// Converts a Speckle Plane object to a Rhino Plane object.
  /// </summary>
  /// <param name="target">The Speckle Plane object to be converted.</param>
  /// <returns>The converted Rhino Plane object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.Plane Convert(SOG.Plane target) =>
    new(
      _pointConverter.Convert(target.origin),
      _vectorConverter.Convert(target.xdir),
      _vectorConverter.Convert(target.ydir)
    );
}

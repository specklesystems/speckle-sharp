using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpecklePlaneToHostConverter : IRawConversion<SOG.Plane, RG.Plane>
{
  private readonly IRawConversion<SOG.Point, RG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, RG.Vector3d> _vectorConverter;

  public SpecklePlaneToHostConverter(
    IRawConversion<SOG.Point, RG.Point3d> pointConverter,
    IRawConversion<SOG.Vector, RG.Vector3d> vectorConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  // POC: Super simplified, must check original conversion
  public RG.Plane RawConvert(SOG.Plane target) =>
    new(
      _pointConverter.RawConvert(target.origin),
      _vectorConverter.RawConvert(target.xdir),
      _vectorConverter.RawConvert(target.ydir)
    );
}

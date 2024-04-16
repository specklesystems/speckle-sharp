using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleCircleRawToHostConversion
  : IRawConversion<SOG.Circle, RG.Circle>,
    IRawConversion<SOG.Circle, RG.ArcCurve>
{
  private readonly IRawConversion<SOG.Plane, RG.Plane> _planeConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpeckleCircleRawToHostConversion(
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter,
    IRawConversion<SOG.Plane, RG.Plane> planeConverter
  )
  {
    _intervalConverter = intervalConverter;
    _planeConverter = planeConverter;
  }

  public RG.Circle RawConvert(SOG.Circle target)
  {
    if (target.radius == null)
    {
      // POC: CNX-9272 Circle radius being nullable makes no sense
      throw new ArgumentNullException(nameof(target), "Circle radius cannot be null");
    }

    var plane = _planeConverter.RawConvert(target.plane);
    var radius = target.radius.Value;
    return new RG.Circle(plane, radius);
  }

  RG.ArcCurve IRawConversion<SOG.Circle, RG.ArcCurve>.RawConvert(SOG.Circle target) =>
    new(RawConvert(target)) { Domain = _intervalConverter.RawConvert(target.domain) };
}

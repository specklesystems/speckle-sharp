using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class ArcToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Arc, SOG.Arc>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;

  public ArcToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Plane, SOG.Plane> planeConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter
  )
  {
    _pointConverter = pointConverter;
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
  }

  public Base Convert(object target) => RawConvert((RG.Arc)target);

  public SOG.Arc RawConvert(RG.Arc target) =>
    // TODO: handle conversions that define Radius1/Radius2 as major/minor instead of xaxis/yaxis
    new(
      _planeConverter.RawConvert(target.Plane),
      target.Radius,
      target.StartAngle,
      target.EndAngle,
      target.Angle,
      Units.Meters
    )
    {
      startPoint = _pointConverter.RawConvert(target.StartPoint),
      midPoint = _pointConverter.RawConvert(target.MidPoint),
      endPoint = _pointConverter.RawConvert(target.EndPoint),
      domain = new SOP.Interval(0, 1),
      length = target.Length,
      bbox = _boxConverter.RawConvert(new RG.Box(target.BoundingBox()))
    };
}

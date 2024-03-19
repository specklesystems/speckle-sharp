using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Objects.Primitives;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Box), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BoxToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Box, SOG.Box>
{
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<RG.Interval, Interval> _intervalConverter;

  public BoxToSpeckleConverter(
    IRawConversion<RG.Plane, SOG.Plane> planeConverter,
    IRawConversion<RG.Interval, Interval> intervalConverter
  )
  {
    _planeConverter = planeConverter;
    _intervalConverter = intervalConverter;
  }

  public Base Convert(object target) => RawConvert((RG.Box)target);

  public SOG.Box RawConvert(RG.Box target) =>
    new(
      _planeConverter.RawConvert(target.Plane),
      _intervalConverter.RawConvert(target.X),
      _intervalConverter.RawConvert(target.Y),
      _intervalConverter.RawConvert(target.Z),
      Units.Meters
    )
    {
      area = target.Area,
      volume = target.Volume
    };
}

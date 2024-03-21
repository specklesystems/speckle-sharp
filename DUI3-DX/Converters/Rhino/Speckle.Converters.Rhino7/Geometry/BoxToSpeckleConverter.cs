using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Box), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BoxToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Box, SOG.Box>
{
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public BoxToSpeckleConverter(
    IRawConversion<RG.Plane, SOG.Plane> planeConverter,
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _planeConverter = planeConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Box)target);

  public SOG.Box RawConvert(RG.Box target) =>
    new(
      _planeConverter.RawConvert(target.Plane),
      _intervalConverter.RawConvert(target.X),
      _intervalConverter.RawConvert(target.Y),
      _intervalConverter.RawConvert(target.Z),
      _contextStack.Current.SpeckleUnits
    )
    {
      area = target.Area,
      volume = target.Volume
    };
}

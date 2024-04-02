using ArcGIS.Core.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Geometry;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Geometry;

/*
[NameAndRankValue(nameof(Envelope), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BoxToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Envelope, Box>
{
  
  private readonly IRawConversion<RG.Plane, Plane> _planeConverter;
  private readonly IRawConversion<RG.Interval, Interval> _intervalConverter;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public BoxToSpeckleConverter(
    IRawConversion<RG.Plane, Plane> planeConverter,
    IRawConversion<RG.Interval, Interval> intervalConverter,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _planeConverter = planeConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((Envelope)target);

  public Box RawConvert(Envelope target) =>
    new(
      _planeConverter.RawConvert(target.Plane),
      _intervalConverter.RawConvert(target.X),
      _intervalConverter.RawConvert(target.Y),
      _intervalConverter.RawConvert(target.Z),
      _contextStack.Current.SpeckleUnits
    )
    {
      area = target.Area,
      volume = 0
    };
}
  
*/

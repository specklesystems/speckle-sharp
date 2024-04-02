using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(CubicBezierSegment), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BezierSegmentToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<CubicBezierSegment, SOG.Point>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public BezierSegmentToSpeckleConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((CubicBezierSegment)target);

  public SOG.Point RawConvert(CubicBezierSegment target) => new();
}

using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(Multipoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MultipointFeatureToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<Multipoint, List<Base>>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<ArcMapPoint, SOG.Point> _pointConverter;

  public MultipointFeatureToSpeckleConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<ArcMapPoint, SOG.Point> pointConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
  }

  public List<Base> Convert(object target) => RawConvert((Multipoint)target);

  public List<Base> RawConvert(Multipoint target)
  {
    List<Base> multipoint = new();
    foreach (ArcMapPoint point in target.Points)
    {
      multipoint.Add(_pointConverter.RawConvert(point));
    }

    return multipoint;
  }
}

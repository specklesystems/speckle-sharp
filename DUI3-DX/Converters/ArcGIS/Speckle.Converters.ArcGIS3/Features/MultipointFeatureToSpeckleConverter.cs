using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(ACG.Multipoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MultipointFeatureToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<ACG.Multipoint, Base>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<ArcMapPoint, SOG.Point> _pointConverter;

  public MultipointFeatureToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<ArcMapPoint, SOG.Point> pointConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((ACG.Multipoint)target);

  public Base RawConvert(ACG.Multipoint target)
  {
    List<Base> multipoint = new();
    foreach (ArcMapPoint point in target.Points)
    {
      multipoint.Add(_pointConverter.RawConvert(point));
    }

    return multipoint[0];
  }
}

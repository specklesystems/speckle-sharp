using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(GisFeature), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class GisFeatureToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<GisFeature, object>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<SOG.Polyline, Polyline> _polylineConverter;

  public GisFeatureToHostConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<SOG.Polyline, Polyline> polylineConverter
  )
  {
    _contextStack = contextStack;
    _polylineConverter = polylineConverter;
  }

  public object Convert(Base target) => RawConvert((GisFeature)target);

  public object RawConvert(GisFeature target)
  {
    return new();
  }
}

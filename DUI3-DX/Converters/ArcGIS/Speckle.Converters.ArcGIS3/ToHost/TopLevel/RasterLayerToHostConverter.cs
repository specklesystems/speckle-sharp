using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.ToHost.TopLevel;

[NameAndRankValue(nameof(RasterLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RasterLayerToHostConverter : IToHostTopLevelConverter, ITypedConverter<RasterLayer, string>
{
  public object Convert(Base target) => Convert((RasterLayer)target);

  public string Convert(RasterLayer target)
  {
    // POC:
    throw new NotImplementedException($"Receiving Rasters is not supported");
  }
}

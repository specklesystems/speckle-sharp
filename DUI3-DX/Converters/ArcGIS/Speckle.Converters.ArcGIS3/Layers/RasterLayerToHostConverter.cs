using Objects.GIS;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(RasterLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RasterLayerToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<RasterLayer, string>
{
  public object Convert(Base target) => RawConvert((RasterLayer)target);

  public string RawConvert(RasterLayer target)
  {
    // POC:
    throw new NotImplementedException($"Receiving Rasters is not supported");
  }
}

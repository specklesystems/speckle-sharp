using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LevelConversionToSpeckle : ITypedConverter<DB.Level, SOBR.RevitLevel>
{
  private readonly ScalingServiceToSpeckle _scalingService;

  public LevelConversionToSpeckle(ScalingServiceToSpeckle scalingService)
  {
    _scalingService = scalingService;
  }

  public SOBR.RevitLevel RawConvert(DB.Level target)
  {
    SOBR.RevitLevel level =
      new()
      {
        elevation = _scalingService.ScaleLength(target.Elevation),
        name = target.Name,
        createView = true
      };

    return level;
  }
}

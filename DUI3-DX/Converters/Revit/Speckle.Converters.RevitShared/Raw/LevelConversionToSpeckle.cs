using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LevelConversionToSpeckle : IRawConversion<DB.Level, SOBR.RevitLevel>
{
  private readonly ToSpeckleScalingService _scalingService;

  public LevelConversionToSpeckle(ToSpeckleScalingService scalingService)
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

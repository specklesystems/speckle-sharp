using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Level), 0)]
public class LevelConversionToSpeckle : BaseTopLevelConverterToSpeckle<DB.Level, SOBR.RevitLevel>
{
  private readonly ScalingServiceToSpeckle _scalingService;

  public LevelConversionToSpeckle(ScalingServiceToSpeckle scalingService)
  {
    _scalingService = scalingService;
  }

  public override SOBR.RevitLevel Convert(DB.Level target)
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

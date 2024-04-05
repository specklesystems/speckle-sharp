using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LevelConversionToSpeckle : IRawConversion<DB.Level, SOBR.RevitLevel>
{
  private readonly CachingService _cachingService;
  private readonly ToSpeckleScalingService _scalingService;

  public LevelConversionToSpeckle(CachingService cachingService, ToSpeckleScalingService scalingService)
  {
    _cachingService = cachingService;
    _scalingService = scalingService;
  }

  public SOBR.RevitLevel RawConvert(DB.Level target)
  {
    return _cachingService.GetOrAdd(target.UniqueId, () => CreateSpeckleRevitLevel(target));
  }

  private SOBR.RevitLevel CreateSpeckleRevitLevel(DB.Level level)
  {
    var speckleLevel = new SOBR.RevitLevel
    {
      elevation = _scalingService.ScaleLength(level.Elevation),
      name = level.Name,
      createView = true
    };

    // GetAllRevitParamsAndIds(speckleLevel, revitLevel);

    return speckleLevel;
  }
}

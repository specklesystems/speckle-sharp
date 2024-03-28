using Objects.BuiltElements.Revit;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LevelConverter { }

[NameAndRankValue(nameof(DB.Level), 0)]
public class LevelConversionToSpeckle : BaseConversionToSpeckle<DB.Level, RevitLevel>
{
  private readonly CachingService _cachingService;
  private readonly ToSpeckleScalingService _scalingService;

  public LevelConversionToSpeckle(CachingService cachingService, ToSpeckleScalingService scalingService)
  {
    _cachingService = cachingService;
    _scalingService = scalingService;
  }

  public override RevitLevel RawConvert(DB.Level target)
  {
    return _cachingService.GetOrAdd(target.UniqueId, () => CreateSpeckleRevitLevel(target));
  }

  private RevitLevel CreateSpeckleRevitLevel(DB.Level level)
  {
    var speckleLevel = new RevitLevel
    {
      elevation = _scalingService.ScaleLength(level.Elevation),
      name = level.Name,
      createView = true
    };

    // GetAllRevitParamsAndIds(speckleLevel, revitLevel);

    return speckleLevel;
  }
}

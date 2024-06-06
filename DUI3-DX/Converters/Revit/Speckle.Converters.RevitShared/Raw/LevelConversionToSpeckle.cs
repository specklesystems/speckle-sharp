using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LevelConversionToSpeckle : ITypedConverter<DB.Level, SOBR.RevitLevel>
{
  private readonly IScalingServiceToSpeckle _scalingService;

  public LevelConversionToSpeckle(IScalingServiceToSpeckle scalingService)
  {
    _scalingService = scalingService;
  }

  public SOBR.RevitLevel Convert(DB.Level target)
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

using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared;

public class LevelConversionToSpeckle : ITypedConverter<IRevitLevel, SOBR.RevitLevel>
{
  private readonly IScalingServiceToSpeckle _scalingService;

  public LevelConversionToSpeckle(IScalingServiceToSpeckle scalingService)
  {
    _scalingService = scalingService;
  }

  public SOBR.RevitLevel Convert(IRevitLevel target)
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

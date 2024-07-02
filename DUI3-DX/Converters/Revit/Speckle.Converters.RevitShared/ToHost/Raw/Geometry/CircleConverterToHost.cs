using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Core.Common;

namespace Speckle.Converters.RevitShared.ToHost.Raw.Geometry;

public class CircleConverterToHost : ITypedConverter<SOG.Circle, DB.Arc>
{
  private readonly ScalingServiceToHost _scalingService;
  private readonly ITypedConverter<SOG.Plane, DB.Plane> _planeConverter;

  public CircleConverterToHost(ScalingServiceToHost scalingService, ITypedConverter<SOG.Plane, DB.Plane> planeConverter)
  {
    _scalingService = scalingService;
    _planeConverter = planeConverter;
  }

  public DB.Arc Convert(SOG.Circle target)
  {
    var plane = _planeConverter.Convert(target.plane);
    return DB.Arc.Create(
      plane,
      _scalingService.ScaleToNative((double)target.radius.NotNull(), target.units),
      0,
      2 * Math.PI
    );
  }
}

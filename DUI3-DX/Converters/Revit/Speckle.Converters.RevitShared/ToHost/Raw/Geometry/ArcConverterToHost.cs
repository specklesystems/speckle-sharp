using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Core.Common;

namespace Speckle.Converters.RevitShared.ToHost.Raw.Geometry;

public class ArcConverterToHost : ITypedConverter<SOG.Arc, DB.Arc>
{
  private readonly ScalingServiceToHost _scalingService;
  private readonly ITypedConverter<SOG.Point, DB.XYZ> _pointToXyzConverter;
  private readonly ITypedConverter<SOG.Plane, DB.Plane> _planeConverter;

  public ArcConverterToHost(
    ITypedConverter<SOG.Point, DB.XYZ> pointToXyzConverter,
    ScalingServiceToHost scalingService,
    ITypedConverter<SOG.Plane, DB.Plane> planeConverter
  )
  {
    _pointToXyzConverter = pointToXyzConverter;
    _scalingService = scalingService;
    _planeConverter = planeConverter;
  }

  public DB.Arc Convert(SOG.Arc target)
  {
    double startAngle;
    double endAngle;

    if (target.startAngle > target.endAngle)
    {
      startAngle = (double)target.endAngle;
      endAngle = (double)target.startAngle;
    }
    else
    {
      startAngle = (double)target.startAngle.NotNull();
      endAngle = (double)target.endAngle.NotNull();
    }

    var plane = _planeConverter.Convert(target.plane);

    if (SOG.Point.Distance(target.startPoint, target.endPoint) < 1E-6)
    {
      // Endpoints coincide, it's a circle.
      return DB.Arc.Create(
        plane,
        _scalingService.ScaleToNative(target.radius ?? 0, target.units),
        startAngle,
        endAngle
      );
    }

    return DB.Arc.Create(
      _pointToXyzConverter.Convert(target.startPoint),
      _pointToXyzConverter.Convert(target.endPoint),
      _pointToXyzConverter.Convert(target.midPoint)
    );
  }
}

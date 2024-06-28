using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PointConverterToHost : ITypedConverter<SOG.Point, DB.XYZ>
{
  private readonly ScalingServiceToHost _scalingService;
  private readonly IReferencePointConverter _referencePointConverter;

  public PointConverterToHost(ScalingServiceToHost scalingService, IReferencePointConverter referencePointConverter)
  {
    _scalingService = scalingService;
    _referencePointConverter = referencePointConverter;
  }

  public XYZ Convert(SOG.Point target)
  {
    var revitPoint = new XYZ(
      _scalingService.ScaleToNative(target.x, target.units),
      _scalingService.ScaleToNative(target.y, target.units),
      _scalingService.ScaleToNative(target.z, target.units)
    );
    return _referencePointConverter.ToInternalCoordinates(revitPoint, true);
  }
}

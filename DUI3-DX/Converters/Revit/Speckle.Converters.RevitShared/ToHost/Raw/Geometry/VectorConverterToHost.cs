using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class VectorConverterToHost : ITypedConverter<SOG.Vector, DB.XYZ>
{
  private readonly ScalingServiceToHost _scalingService;
  private readonly IReferencePointConverter _referencePointConverter;

  public VectorConverterToHost(ScalingServiceToHost scalingService, IReferencePointConverter referencePointConverter)
  {
    _scalingService = scalingService;
    _referencePointConverter = referencePointConverter;
  }

  public XYZ Convert(Vector target)
  {
    var revitVector = new XYZ(
      _scalingService.ScaleToNative(target.x, target.units),
      _scalingService.ScaleToNative(target.y, target.units),
      _scalingService.ScaleToNative(target.z, target.units)
    );
    return _referencePointConverter.ToInternalCoordinates(revitVector, false);
  }
}

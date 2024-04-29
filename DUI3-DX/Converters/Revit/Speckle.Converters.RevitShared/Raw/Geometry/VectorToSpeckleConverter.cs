using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class VectorToSpeckleConverter : IRawConversion<DB.XYZ, SOG.Vector>
{
  private readonly IReferencePointConverter _referencePointConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public VectorToSpeckleConverter(
    IReferencePointConverter referencePointConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _referencePointConverter = referencePointConverter;
    _scalingService = scalingService;
  }

  public SOG.Vector RawConvert(DB.XYZ target)
  {
    // POC: originally had a concept of not transforming, but this was
    // optional arg defaulting to false - removing the argument appeared to break nothing
    DB.XYZ extPt = _referencePointConverter.ConvertToExternalCoordindates(target, false);
    var pointToSpeckle = new SOG.Vector(
      _scalingService.ScaleLength(extPt.X),
      _scalingService.ScaleLength(extPt.Y),
      _scalingService.ScaleLength(extPt.Z)
    );

    return pointToSpeckle;
  }
}

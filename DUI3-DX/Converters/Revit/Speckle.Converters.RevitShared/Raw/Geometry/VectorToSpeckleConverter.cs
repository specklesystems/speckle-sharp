using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class VectorToSpeckleConverter : IRawConversion<DB.XYZ, SOG.Vector>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IReferencePointConverter _referencePointConverter;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public VectorToSpeckleConverter(
    IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter,
    IRevitConversionContextStack contextStack,
    IReferencePointConverter _referencePointConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _scalingService = scalingService;
  }

  public SOG.Vector RawConvert(DB.XYZ xyz)
  {
    // POC: originally had a concept of not transforming, but this was
    // optional arg defaulting to false - removing the argument appeared to break nothing
    DB.XYZ extPt = _referencePointConverter.ConvertToExternalCoordindates(xyz, false);
    var pointToSpeckle = new SOG.Vector(
      _scalingService.ScaleLength(extPt.X),
      _scalingService.ScaleLength(extPt.Y),
      _scalingService.ScaleLength(extPt.Z)
    );

    return pointToSpeckle;
  }
}

using Speckle.Converters.Common.Objects;
using Speckle.Converters.Revit2023;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Api;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class VectorToSpeckleConverter : ITypedConverter<DB.XYZ, SOG.Vector>
{
  private readonly IReferencePointConverter _referencePointConverter;
  private readonly IScalingServiceToSpeckle _scalingService;

  public VectorToSpeckleConverter(
    IReferencePointConverter referencePointConverter,
    IScalingServiceToSpeckle scalingService
  )
  {
    _referencePointConverter = referencePointConverter;
    _scalingService = scalingService;
  }

  public SOG.Vector Convert(DB.XYZ target)
  {
    // POC: originally had a concept of not transforming, but this was
    // optional arg defaulting to false - removing the argument appeared to break nothing
    var extPt = _referencePointConverter.ConvertToExternalCoordindates(new XYZProxy(target), false);
    var pointToSpeckle = new SOG.Vector(
      _scalingService.ScaleLength(extPt.X),
      _scalingService.ScaleLength(extPt.Y),
      _scalingService.ScaleLength(extPt.Z)
    );

    return pointToSpeckle;
  }
}

using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class VectorToSpeckleConverter : ITypedConverter<IRevitXYZ, SOG.Vector>
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

  public SOG.Vector Convert(IRevitXYZ target)
  {
    // POC: originally had a concept of not transforming, but this was
    // optional arg defaulting to false - removing the argument appeared to break nothing
    var extPt = _referencePointConverter.ConvertToExternalCoordindates(target, false);
    var pointToSpeckle = new SOG.Vector(
      _scalingService.ScaleLength(extPt.X),
      _scalingService.ScaleLength(extPt.Y),
      _scalingService.ScaleLength(extPt.Z)
    );

    return pointToSpeckle;
  }
}

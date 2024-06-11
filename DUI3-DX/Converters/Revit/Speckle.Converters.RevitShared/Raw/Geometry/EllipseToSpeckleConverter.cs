using Objects.Primitive;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class EllipseToSpeckleConverter : ITypedConverter<IRevitEllipse, SOG.Ellipse>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitPlane, SOG.Plane> _planeConverter;
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly IRevitPlaneUtils _revitPlaneUtils;

  public EllipseToSpeckleConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitPlane, SOG.Plane> planeConverter,
    IScalingServiceToSpeckle scalingService, IRevitPlaneUtils revitPlaneUtils)
  {
    _contextStack = contextStack;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
    _revitPlaneUtils = revitPlaneUtils;
  }

  public SOG.Ellipse Convert(IRevitEllipse target)
  {
    using (IRevitPlane basePlane = _revitPlaneUtils.CreateByOriginAndBasis(target.Center, target.XDirection, target.YDirection))
    {
      var trim = target.IsBound ? new Interval(target.GetEndParameter(0), target.GetEndParameter(1)) : null;

      return new SOG.Ellipse()
      {
        plane = _planeConverter.Convert(basePlane),
        // POC: scale length correct? seems right?
        firstRadius = _scalingService.ScaleLength(target.RadiusX),
        secondRadius = _scalingService.ScaleLength(target.RadiusY),
        // POC: original EllipseToSpeckle() method was setting this twice
        domain = new Interval(0, 1),
        trimDomain = trim,
        length = _scalingService.ScaleLength(target.Length),
        units = _contextStack.Current.SpeckleUnits
      };
    }
  }
}

using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class EllipseToSpeckleConverter : ITypedConverter<DB.Ellipse, SOG.Ellipse>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.Plane, SOG.Plane> _planeConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public EllipseToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.Plane, SOG.Plane> planeConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _contextStack = contextStack;
    _planeConverter = planeConverter;
    _scalingService = scalingService;
  }

  public SOG.Ellipse RawConvert(DB.Ellipse target)
  {
    using (DB.Plane basePlane = DB.Plane.CreateByOriginAndBasis(target.Center, target.XDirection, target.YDirection))
    {
      var trim = target.IsBound ? new Interval(target.GetEndParameter(0), target.GetEndParameter(1)) : null;

      return new SOG.Ellipse()
      {
        plane = _planeConverter.RawConvert(basePlane),
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

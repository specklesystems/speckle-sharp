using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class EllipseToSpeckleConverter : IRawConversion<DB.Ellipse, SOG.Ellipse>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.Plane, SOG.Plane> _planeConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public EllipseToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    IRawConversion<DB.Plane, SOG.Plane> planeConverter,
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

      var ellipseToSpeckle = new SOG.Ellipse(
        _planeConverter.RawConvert(basePlane),
        // POC: scale length correct? seems right?
        _scalingService.ScaleLength(target.RadiusX),
        _scalingService.ScaleLength(target.RadiusY),
        new Interval(0, 2 * Math.PI),
        trim,
        _contextStack.Current.SpeckleUnits
      );

      // POC: correct way to scale?
      ellipseToSpeckle.length = _scalingService.ScaleLength(target.Length);
      ellipseToSpeckle.domain = new Interval(0, 1);

      return ellipseToSpeckle;
    }
  }
}

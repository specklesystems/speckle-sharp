using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.Raw;

internal class ModelCurveArrayToSpeckleConverter : IRawConversion<DB.ModelCurveArray, SOG.Polycurve>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly ScalingServiceToSpeckle _scalingService;
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;

  public ModelCurveArrayToSpeckleConverter(
    RevitConversionContextStack contextStack,
    ScalingServiceToSpeckle scalingService,
    IRawConversion<DB.Curve, ICurve> curveConverter
  )
  {
    _contextStack = contextStack;
    _scalingService = scalingService;
    _curveConverter = curveConverter;
  }

  public SOG.Polycurve RawConvert(DB.ModelCurveArray target)
  {
    SOG.Polycurve polycurve = new();
    var curves = target.Cast<DB.ModelCurve>().Select(mc => mc.GeometryCurve).ToArray();

    if (curves.Length == 0)
    {
      throw new SpeckleConversionException($"Expected {target} to have at least 1 curve");
    }

    var start = curves[0].GetEndPoint(0);
    var end = curves[curves.Length - 1].GetEndPoint(1);
    polycurve.units = _contextStack.Current.SpeckleUnits;
    polycurve.closed = start.DistanceTo(end) < RevitConversionContextStack.TOLERANCE;
    polycurve.length = _scalingService.ScaleLength(curves.Sum(x => x.Length));

    polycurve.segments.AddRange(curves.Select(x => _curveConverter.RawConvert(x)));

    return polycurve;
  }
}

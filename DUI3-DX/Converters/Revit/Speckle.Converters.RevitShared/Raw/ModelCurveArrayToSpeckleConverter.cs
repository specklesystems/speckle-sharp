using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.Raw;

public sealed class ModelCurveArrayToSpeckleConverter : ITypedConverter<DB.ModelCurveArray, SOG.Polycurve>, ITypedConverter<IRevitModelCurveCollection, SOG.Polycurve>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter;

  public ModelCurveArrayToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    IScalingServiceToSpeckle scalingService,
    ITypedConverter<IRevitCurve, ICurve> curveConverter
  )
  {
    _contextStack = contextStack;
    _scalingService = scalingService;
    _curveConverter = curveConverter;
  }

  public SOG.Polycurve Convert(IRevitModelCurveCollection target)
  {
    SOG.Polycurve polycurve = new();
    var curves = target.Cast().Select(mc => mc.GeometryCurve).ToArray();

    if (curves.Length == 0)
    {
      throw new SpeckleConversionException($"Expected {target} to have at least 1 curve");
    }

    var start = curves[0].GetEndPoint(0);
    var end = curves[^1].GetEndPoint(1);
    polycurve.units = _contextStack.Current.SpeckleUnits;
    polycurve.closed = start.DistanceTo(end) < RevitConversionContextStack.TOLERANCE;
    polycurve.length = _scalingService.ScaleLength(curves.Sum(x => x.Length));

    polycurve.segments.AddRange(curves.Select(x => _curveConverter.Convert(x)));

    return polycurve;
  }

  public SOG.Polycurve Convert(DB.ModelCurveArray target) => Convert(new ModelCurveArrayProxy(target));
}

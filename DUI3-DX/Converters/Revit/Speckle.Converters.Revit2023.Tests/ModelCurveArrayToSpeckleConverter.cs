using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit2023.Interfaces;
using SOG = Objects.Geometry;

namespace Speckle.Converters.Revit2023.Tests;

public sealed class ModelCurveArrayToSpeckleConverter : ITypedConverter<IRevitModelCurveCollection, SOG.Polycurve>
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
    var curves = target.Select(mc => mc.GeometryCurve).ToArray();

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
}

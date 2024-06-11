using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;
#pragma warning disable IDE0130
namespace Speckle.Converters.Revit2023;

public sealed class ModelCurveArrayToSpeckleConverter : ITypedConverter<IRevitModelCurveArray, SOG.Polycurve>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter;

  public ModelCurveArrayToSpeckleConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    IScalingServiceToSpeckle scalingService,
    ITypedConverter<IRevitCurve, ICurve> curveConverter
  )
  {
    _contextStack = contextStack;
    _scalingService = scalingService;
    _curveConverter = curveConverter;
  }

  public SOG.Polycurve Convert(IRevitModelCurveArray target) => Convert((IReadOnlyList<IRevitModelCurve>)target);

  public SOG.Polycurve Convert(IReadOnlyList<IRevitModelCurve> target)
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
    polycurve.closed = start.DistanceTo(end) < RevitConstants.TOLERANCE;
    polycurve.length = _scalingService.ScaleLength(curves.Sum(x => x.Length));

    polycurve.segments.AddRange(curves.Select(x => _curveConverter.Convert(x)));

    return polycurve;
  }
}

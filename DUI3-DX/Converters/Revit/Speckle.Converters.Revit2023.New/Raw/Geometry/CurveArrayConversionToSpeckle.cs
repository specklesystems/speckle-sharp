using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Services;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public sealed class CurveArrayConversionToSpeckle : ITypedConverter<IRevitCurveArray, SOG.Polycurve>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter;

  public CurveArrayConversionToSpeckle(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    IScalingServiceToSpeckle scalingService,
    ITypedConverter<IRevitCurve, ICurve> curveConverter
  )
  {
    _contextStack = contextStack;
    _scalingService = scalingService;
    _curveConverter = curveConverter;
  }

  public SOG.Polycurve Convert(IRevitCurveArray target)
  {
    List<IRevitCurve> curves = target.Cast<IRevitCurve>().ToList();

    return new SOG.Polycurve()
    {
      units = _contextStack.Current.SpeckleUnits,
      closed =
        curves.First().GetEndPoint(0).DistanceTo(curves.Last().GetEndPoint(1)) < RevitConstants.TOLERANCE,
      length = _scalingService.ScaleLength(curves.Sum(x => x.Length)),
      segments = curves.Select(x => _curveConverter.Convert(x)).ToList()
    };
  }
}

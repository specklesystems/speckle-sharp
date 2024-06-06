using Autodesk.Revit.DB;
using Objects;
using Objects.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public sealed class CurveArrayConversionToSpeckle : ITypedConverter<DB.CurveArray, SOG.Polycurve>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IScalingServiceToSpeckle _scalingService;
  private readonly ITypedConverter<DB.Curve, ICurve> _curveConverter;

  public CurveArrayConversionToSpeckle(
    IRevitConversionContextStack contextStack,
    IScalingServiceToSpeckle scalingService,
    ITypedConverter<DB.Curve, ICurve> curveConverter
  )
  {
    _contextStack = contextStack;
    _scalingService = scalingService;
    _curveConverter = curveConverter;
  }

  public Polycurve Convert(CurveArray target)
  {
    List<DB.Curve> curves = target.Cast<DB.Curve>().ToList();

    return new Polycurve()
    {
      units = _contextStack.Current.SpeckleUnits,
      closed =
        curves.First().GetEndPoint(0).DistanceTo(curves.Last().GetEndPoint(1)) < RevitConversionContextStack.TOLERANCE,
      length = _scalingService.ScaleLength(curves.Sum(x => x.Length)),
      segments = curves.Select(x => _curveConverter.Convert(x)).ToList()
    };
  }
}

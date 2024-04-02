using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects;
using Objects.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public sealed class CurveArrayConversionToSpeckle : BaseConversionToSpeckle<DB.CurveArray, SOG.Polycurve>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly ToSpeckleScalingService _scalingService;
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;

  public CurveArrayConversionToSpeckle(
    RevitConversionContextStack contextStack,
    ToSpeckleScalingService scalingService,
    IRawConversion<DB.Curve, ICurve> curveConverter
  )
  {
    _contextStack = contextStack;
    _scalingService = scalingService;
    _curveConverter = curveConverter;
  }

  public override Polycurve RawConvert(CurveArray target)
  {
    Polycurve polycurve = new();

    List<DB.Curve> curves = target.Cast<DB.Curve>().ToList();

    polycurve.units = _contextStack.Current.SpeckleUnits;
    polycurve.closed =
      curves.First().GetEndPoint(0).DistanceTo(curves.Last().GetEndPoint(1)) < RevitConversionContextStack.TOLERANCE;
    polycurve.length = _scalingService.ScaleLength(curves.Sum(x => x.Length));

    polycurve.segments.AddRange(curves.Select(x => _curveConverter.RawConvert(x)));
    return polycurve;
  }
}

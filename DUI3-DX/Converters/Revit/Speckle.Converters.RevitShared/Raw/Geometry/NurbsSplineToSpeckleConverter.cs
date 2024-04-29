using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class NurbsSplineToSpeckleConverter : IRawConversion<DB.NurbSpline, SOG.Curve>
{
  private readonly IRevitVersionConversionHelper _conversionHelper;
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public NurbsSplineToSpeckleConverter(
    IRevitVersionConversionHelper conversionHelper,
    IRevitConversionContextStack contextStack,
    IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _conversionHelper = conversionHelper;
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _scalingService = scalingService;
  }

  public SOG.Curve RawConvert(DB.NurbSpline target)
  {
    var units = _contextStack.Current.SpeckleUnits;

    var points = new List<double>();
    foreach (var p in target.CtrlPoints)
    {
      var point = _xyzToPointConverter.RawConvert(p);
      points.AddRange(new List<double> { point.x, point.y, point.z });
    }

    SOG.Curve speckleCurve =
      new()
      {
        weights = target.Weights.Cast<double>().ToList(),
        points = points,
        knots = target.Knots.Cast<double>().ToList()
      };
    ;
    speckleCurve.degree = target.Degree;
    //speckleCurve.periodic = revitCurve.Period; // POC: already commented out, remove?
    speckleCurve.rational = target.isRational;
    speckleCurve.closed = _conversionHelper.IsCurveClosed(target);
    speckleCurve.units = units;
    speckleCurve.domain = new Interval(target.GetEndParameter(0), target.GetEndParameter(1));
    speckleCurve.length = _scalingService.ScaleLength(target.Length);

    var coords = target.Tessellate().SelectMany(xyz => _xyzToPointConverter.RawConvert(xyz).ToList()).ToList();
    speckleCurve.displayValue = new SOG.Polyline(coords, units);

    return speckleCurve;
  }
}

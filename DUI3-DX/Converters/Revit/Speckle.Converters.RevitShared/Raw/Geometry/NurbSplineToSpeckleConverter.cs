using Objects;
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

  public SOG.Curve RawConvert(DB.NurbSpline nurbsSpline)
  {
    var units = _contextStack.Current.SpeckleUnits;

    var points = new List<double>();
    foreach (var p in nurbsSpline.CtrlPoints)
    {
      var point = _xyzToPointConverter.RawConvert(p);
      points.AddRange(new List<double> { point.x, point.y, point.z });
    }

    SOG.Curve speckleCurve = new();
    speckleCurve.weights = nurbsSpline.Weights.Cast<double>().ToList();
    speckleCurve.points = points;
    speckleCurve.knots = nurbsSpline.Knots.Cast<double>().ToList();
    ;
    speckleCurve.degree = nurbsSpline.Degree;
    //speckleCurve.periodic = revitCurve.Period; // POC: already commented out, remove?
    speckleCurve.rational = nurbsSpline.isRational;
    speckleCurve.closed = _conversionHelper.IsCurveClosed(nurbsSpline);
    speckleCurve.units = units;
    speckleCurve.domain = new Interval(nurbsSpline.GetEndParameter(0), nurbsSpline.GetEndParameter(1));
    speckleCurve.length = _scalingService.ScaleLength(nurbsSpline.Length);

    var coords = nurbsSpline.Tessellate().SelectMany(xyz => _xyzToPointConverter.RawConvert(xyz).ToList()).ToList();
    speckleCurve.displayValue = new SOG.Polyline(coords, units);

    return speckleCurve;
  }
}

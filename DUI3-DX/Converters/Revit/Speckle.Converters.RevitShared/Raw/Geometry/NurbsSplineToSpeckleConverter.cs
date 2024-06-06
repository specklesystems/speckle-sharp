using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class NurbsSplineToSpeckleConverter : ITypedConverter<DB.NurbSpline, SOG.Curve>
{
  private readonly IRevitVersionConversionHelper _conversionHelper;
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly IScalingServiceToSpeckle _scalingService;

  public NurbsSplineToSpeckleConverter(
    IRevitVersionConversionHelper conversionHelper,
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter,
    IScalingServiceToSpeckle scalingService
  )
  {
    _conversionHelper = conversionHelper;
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _scalingService = scalingService;
  }

  public SOG.Curve Convert(DB.NurbSpline target)
  {
    var units = _contextStack.Current.SpeckleUnits;

    var points = new List<double>();
    foreach (var p in target.CtrlPoints)
    {
      var point = _xyzToPointConverter.Convert(p);
      points.AddRange(new List<double> { point.x, point.y, point.z });
    }

    var coords = target.Tessellate().SelectMany(xyz => _xyzToPointConverter.Convert(xyz).ToList()).ToList();

    return new SOG.Curve()
    {
      weights = target.Weights.Cast<double>().ToList(),
      points = points,
      knots = target.Knots.Cast<double>().ToList(),
      degree = target.Degree,
      //speckleCurve.periodic = revitCurve.Period; // POC: already commented out, remove?
      rational = target.isRational,
      closed = _conversionHelper.IsCurveClosed(target),
      units = units,
      domain = new Interval(target.GetEndParameter(0), target.GetEndParameter(1)),
      length = _scalingService.ScaleLength(target.Length),
      displayValue = new SOG.Polyline(coords, units)
    };
  }
}

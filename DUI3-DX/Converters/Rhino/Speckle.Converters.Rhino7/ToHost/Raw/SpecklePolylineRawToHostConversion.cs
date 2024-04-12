using Rhino.Collections;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpecklePolylineRawToHostConversion
  : IRawConversion<SOG.Polyline, RG.Polyline>,
    IRawConversion<SOG.Polyline, RG.PolylineCurve>
{
  private readonly IRawConversion<IList<double>, Point3dList> _pointListConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpecklePolylineRawToHostConversion(
    IRawConversion<IList<double>, Point3dList> pointListConverter,
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _pointListConverter = pointListConverter;
    _intervalConverter = intervalConverter;
  }

  public RG.Polyline RawConvert(SOG.Polyline target)
  {
    var points = _pointListConverter.RawConvert(target.value);

    if (target.closed)
    {
      points.Add(points[0]);
    }

    var poly = new RG.Polyline(points);

    return poly;
  }

  // POC: Potential code-smell by directly implementing the interface. We should discuss this further but
  // since we're using the interfaces instead of the direct type, this may not be an issue.
  RG.PolylineCurve IRawConversion<SOG.Polyline, RG.PolylineCurve>.RawConvert(SOG.Polyline target)
  {
    var poly = RawConvert(target).ToPolylineCurve();

    if (target.domain != null)
    {
      poly.Domain = _intervalConverter.RawConvert(target.domain);
    }

    return poly;
  }
}

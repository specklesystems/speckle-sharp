using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleArcRawToHostConversion : IRawConversion<SOG.Arc, RG.Arc>, IRawConversion<SOG.Arc, RG.ArcCurve>
{
  private readonly IRawConversion<SOG.Point, RG.Point3d> _pointConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpeckleArcRawToHostConversion(
    IRawConversion<SOG.Point, RG.Point3d> pointConverter,
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _pointConverter = pointConverter;
    this._intervalConverter = intervalConverter;
  }

  public RG.Arc RawConvert(SOG.Arc target)
  {
    var rhinoArc = new RG.Arc(
      _pointConverter.RawConvert(target.startPoint),
      _pointConverter.RawConvert(target.midPoint),
      _pointConverter.RawConvert(target.endPoint)
    );
    return rhinoArc;
  }

  // POC: CNX-9271 Potential code-smell by directly implementing the interface. We should discuss this further but
  // since we're using the interfaces instead of the direct type, this may not be an issue.
  RG.ArcCurve IRawConversion<SOG.Arc, RG.ArcCurve>.RawConvert(SOG.Arc target)
  {
    var rhinoArc = RawConvert(target);
    var arcCurve = new RG.ArcCurve(rhinoArc);

    if (target.domain != null)
    {
      arcCurve.Domain = _intervalConverter.RawConvert(target.domain);
    }

    return arcCurve;
  }
}

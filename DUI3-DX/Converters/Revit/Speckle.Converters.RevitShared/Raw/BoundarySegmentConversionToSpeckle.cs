using Objects;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class BoundarySegmentConversionToSpeckle : IRawConversion<IList<DB.BoundarySegment>, SOG.Polycurve>
{
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;

  public BoundarySegmentConversionToSpeckle(IRawConversion<DB.Curve, ICurve> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public SOG.Polycurve RawConvert(IList<DB.BoundarySegment> target)
  {
    if (!target.Any())
    {
      throw new ArgumentException("Input Boundary segment list must at least have 1 segment");
    }

    var poly = new SOG.Polycurve();
    foreach (var segment in target)
    {
      DB.Curve revitCurve = segment.GetCurve();
      var curve = _curveConverter.RawConvert(revitCurve);

      // POC: Not sure why we would need these to have an ElementID attached.
      // curve["elementId"] = segment.ElementId.ToString();

      poly.segments.Add(curve);
    }

    return poly;
  }
}

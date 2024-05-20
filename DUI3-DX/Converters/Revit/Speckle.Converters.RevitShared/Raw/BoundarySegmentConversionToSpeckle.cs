using Objects;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class BoundarySegmentConversionToSpeckle : ITypedConverter<IList<DB.BoundarySegment>, SOG.Polycurve>
{
  private readonly ITypedConverter<DB.Curve, ICurve> _curveConverter;

  public BoundarySegmentConversionToSpeckle(ITypedConverter<DB.Curve, ICurve> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public SOG.Polycurve RawConvert(IList<DB.BoundarySegment> target)
  {
    if (target.Count == 0)
    {
      throw new ArgumentException("Input Boundary segment list must at least have 1 segment");
    }

    var poly = new SOG.Polycurve();
    foreach (var segment in target)
    {
      DB.Curve revitCurve = segment.GetCurve();
      var curve = _curveConverter.RawConvert(revitCurve);

      // POC: We used to attach the `elementID` of every curve in a PolyCurve as a dynamic property.
      // We've removed this as it seemed unnecessary.

      poly.segments.Add(curve);
    }

    return poly;
  }
}

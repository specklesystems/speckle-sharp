using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023;


public class BoundarySegmentConversionToSpeckle : ITypedConverter<IList<IRevitBoundarySegment>, SOG.Polycurve>
{
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter;

  public BoundarySegmentConversionToSpeckle(ITypedConverter<IRevitCurve, ICurve> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public SOG.Polycurve Convert(IList<IRevitBoundarySegment> target)
  {
    if (target.Count == 0)
    {
      throw new ArgumentException("Input Boundary segment list must at least have 1 segment");
    }

    var poly = new SOG.Polycurve();
    foreach (var segment in target)
    {
      IRevitCurve revitCurve = segment.GetCurve();
      var curve = _curveConverter.Convert(revitCurve);

      // POC: We used to attach the `elementID` of every curve in a PolyCurve as a dynamic property.
      // We've removed this as it seemed unnecessary.

      poly.segments.Add(curve);
    }

    return poly;
  }
}

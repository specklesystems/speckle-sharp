using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleNurbsCurveToHostConversion : IRawConversion<SOG.Curve, RG.NurbsCurve>
{
  private readonly IRawConversion<SOG.Point, RG.Point3d> _pointConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpeckleNurbsCurveToHostConversion(
    IRawConversion<SOG.Point, RG.Point3d> pointConverter,
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _pointConverter = pointConverter;
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts a Speckle NurbsCurve object to a Rhino NurbsCurve object.
  /// </summary>
  /// <param name="target">The Speckle NurbsCurve object to be converted.</param>
  /// <returns>The converted Rhino NurbsCurve object.</returns>
  /// <exception cref="SpeckleConversionException">Thrown when the conversion fails.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.NurbsCurve RawConvert(SOG.Curve target)
  {
    var rhinoPoints = target.GetPoints().Select(o => _pointConverter.RawConvert(o)).ToList();

    RG.NurbsCurve? nurbsCurve = RG.NurbsCurve.Create(false, target.degree, rhinoPoints);

#pragma warning disable CA1508
    if (nurbsCurve == null) // POC: CNX-9272 Nullability is wrong here, cannot remove this warning but code is required.
#pragma warning restore CA1508
    {
      throw new SpeckleConversionException("Attempt to create Nurbs Curve failed with no explanation from Rhino");
    }

    for (int j = 0; j < nurbsCurve.Points.Count; j++)
    {
      nurbsCurve.Points.SetPoint(j, rhinoPoints[j], target.weights[j]);
    }

    // check knot multiplicity to match Rhino's standard of (# control points + degree - 1)
    // skip extra knots at start & end if knot multiplicity is (# control points + degree + 1)
    int extraKnots = target.knots.Count - nurbsCurve.Knots.Count;
    for (int j = 0; j < nurbsCurve.Knots.Count; j++)
    {
      if (extraKnots == 2)
      {
        nurbsCurve.Knots[j] = target.knots[j + 1];
      }
      else
      {
        nurbsCurve.Knots[j] = target.knots[j];
      }
    }

    nurbsCurve.Domain = _intervalConverter.RawConvert(target.domain);
    return nurbsCurve;
  }
}

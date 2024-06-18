using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class NurbsCurveToHostConverter : ITypedConverter<SOG.Curve, IRhinoNurbsCurve>
{
  private readonly IRhinoPointFactory _rhinoPointFactory;
  private readonly IRhinoCurveFactory _rhinoCurveFactory;
  private readonly ITypedConverter<SOP.Interval, IRhinoInterval> _intervalConverter;

  public NurbsCurveToHostConverter(ITypedConverter<SOP.Interval, IRhinoInterval> intervalConverter, IRhinoCurveFactory rhinoCurveFactory, IRhinoPointFactory rhinoPointFactory)
  {
    _intervalConverter = intervalConverter;
    _rhinoCurveFactory = rhinoCurveFactory;
    _rhinoPointFactory = rhinoPointFactory;
  }

  /// <summary>
  /// Converts a Speckle NurbsCurve object to a Rhino NurbsCurve object.
  /// </summary>
  /// <param name="target">The Speckle NurbsCurve object to be converted.</param>
  /// <returns>The converted Rhino NurbsCurve object.</returns>
  /// <exception cref="SpeckleConversionException">Thrown when the conversion fails.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoNurbsCurve Convert(SOG.Curve target)
  {
    IRhinoNurbsCurve nurbsCurve = _rhinoCurveFactory.Create(target.degree, target.points.Count / 3);

    // Hyper optimised curve control point conversion
    for (int i = 2, j = 0; i < target.points.Count; i += 3, j++)
    {
      var pt = _rhinoPointFactory.Create(target.points[i - 2], target.points[i - 1], target.points[i]); // Skip the point converter for performance
      nurbsCurve.Points.SetPoint(j, pt, target.weights[j]);
    }

    // check knot multiplicity to match Rhino's standard of (# control points + degree - 1)
    // skip extra knots at start & end if knot multiplicity is (# control points + degree + 1)
    int extraKnots = target.knots.Count - nurbsCurve.Knots.Count;
    for (int j = 0; j < nurbsCurve.Knots.Count; j++)
    {
      if (extraKnots == 2)
      {
        nurbsCurve.Knots.SetKnot(j, target.knots[j + 1]);
      }
      else
      {
        nurbsCurve.Knots.SetKnot(j,  target.knots[j]);
      }
    }

    nurbsCurve.Domain = _intervalConverter.Convert(target.domain);
    return nurbsCurve;
  }
}

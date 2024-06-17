using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Curve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Curve, ACG.Polyline>
{
  private readonly IRootToHostConverter _converter;
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public CurveToHostConverter(
    IRootToHostConverter converter,
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _converter = converter;
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Curve)target);

  public ACG.Polyline Convert(SOG.Curve target)
  {
    // before we have a better way to recreate periodic curve
    SOG.Polyline segment = target.displayValue;
    return (ACG.Polyline)_converter.Convert(segment);
    /*
    List<ACG.CubicBezierSegment> bezierCurves = ConvertNurbsToBezier(target);

    return new ACG.PolylineBuilderEx(
      bezierCurves,
      ACG.AttributeFlags.HasZ,
      _contextStack.Current.Document.Map.SpatialReference
    ).ToGeometry();
    */
  }

  private List<ACG.CubicBezierSegment> ConvertNurbsToBezier(SOG.Curve nurbsCurve)
  {
    if (nurbsCurve == null || nurbsCurve.points == null || nurbsCurve.knots == null) // || nurbsCurve.weights == null)
    {
      throw new ArgumentNullException(nameof(nurbsCurve), "Invalid Curve provided.");
    }

    List<ACG.CubicBezierSegment> bezierCurves = new();

    // Insert knots to create Bezier segments
    List<double> refinedKnots = RefineKnotVector(nurbsCurve.knots, nurbsCurve.degree);
    List<ACG.MapPoint> refinedControlPoints = InsertKnots(nurbsCurve, refinedKnots);

    // Create Bezier curves from segments
    int numSegments = refinedKnots.Count - 1 - (2 * nurbsCurve.degree);
    for (int i = 0; i < numSegments; i++)
    {
      List<ACG.MapPoint> bezierControlPoints = new();
      for (int j = 0; j < 4; j++)
      {
        bezierControlPoints.Add(refinedControlPoints[i + j]);
      }
      ACG.CubicBezierSegment bezierCurve = new ACG.CubicBezierBuilderEx(
        bezierControlPoints,
        _contextStack.Current.Document.Map.SpatialReference
      ).ToSegment();
      bezierCurves.Add(bezierCurve);
    }

    return bezierCurves;
  }

  private List<double> RefineKnotVector(List<double> knotVector, int degree)
  {
    List<double> refinedKnots = new(knotVector);

    // Insert knots to create Bezier segments
    int n = knotVector.Count - degree - 1;
    for (int i = degree + 1; i < n; i += degree)
    {
      for (int j = 0; j < degree; j++)
      {
        refinedKnots.Insert(i + j + 1, knotVector[i]);
      }
    }

    return refinedKnots;
  }

  private List<ACG.MapPoint> InsertKnots(SOG.Curve nurbsCurve, List<double> refinedKnots)
  {
    List<ACG.MapPoint> refinedControlPoints = new();
    for (int i = 0; i < nurbsCurve.points.Count / 3; i++)
    {
      refinedControlPoints.Add(
        _pointConverter.Convert(
          new SOG.Point(
            nurbsCurve.points[i * 3],
            nurbsCurve.points[i * 3 + 1],
            nurbsCurve.points[i * 3 + 2],
            nurbsCurve.units
          )
        )
      );
    }

    int p = nurbsCurve.degree;
    for (int i = p + 1; i < refinedKnots.Count - p - 1; i++)
    {
      double alpha = (refinedKnots[i] - nurbsCurve.knots[i - p]) / (nurbsCurve.knots[i] - nurbsCurve.knots[i - p]);

      ACG.MapPoint newControlPoint = new ACG.MapPointBuilderEx(
        (1.0 - alpha) * refinedControlPoints[i - p - 1].X + alpha * refinedControlPoints[i - p].X,
        (1.0 - alpha) * refinedControlPoints[i - p - 1].Y + alpha * refinedControlPoints[i - p].Y,
        (1.0 - alpha) * refinedControlPoints[i - p - 1].Z + alpha * refinedControlPoints[i - p].Z,
        _contextStack.Current.Document.Map.SpatialReference
      ).ToGeometry();
      refinedControlPoints.Insert(i, newControlPoint);
    }

    return refinedControlPoints;
  }
}

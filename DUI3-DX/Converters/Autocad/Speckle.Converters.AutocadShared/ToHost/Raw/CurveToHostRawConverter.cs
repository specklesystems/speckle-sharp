using System.Collections.Generic;
using System.Linq;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.AutocadShared.ToHost.Raw;

public class CurveToHostRawConverter : IRawConversion<SOG.Curve, AG.NurbCurve3d>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOP.Interval, AG.Interval> _intervalConverter;

  public CurveToHostRawConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<SOP.Interval, AG.Interval> intervalConverter
  )
  {
    _pointConverter = pointConverter;
    _intervalConverter = intervalConverter;
  }

  public AG.NurbCurve3d RawConvert(SOG.Curve target)
  {
    var points = target.GetPoints().Select(p => _pointConverter.RawConvert(p)).ToList();
    if (target.closed && target.periodic)
    {
      points = points.GetRange(0, points.Count - target.degree);
    }

    var pointCollection = new AG.Point3dCollection(points.ToArray());

    // process knots
    // NOTE: Autocad defines spline knots  as a vector of size # control points + degree + 1. (# at start and end should match degree)
    // Conversions for autocad need to make sure this is satisfied, otherwise will cause protected mem crash.
    // NOTE: for **closed periodic** curves that have "n" control pts, # of knots should be n + 1. Remove degree = 3 knots from start and end.
    List<double> knotList = target.knots;
    if (target.knots.Count == points.Count + target.degree - 1) // handles rhino format curves
    {
      knotList.Insert(0, knotList[0]);
      knotList.Insert(knotList.Count - 1, knotList[knotList.Count - 1]);
    }
    if (target.closed && target.periodic) // handles closed periodic curves
    {
      knotList = knotList.GetRange(target.degree, knotList.Count - target.degree * 2);
    }

    var knots = new AG.KnotCollection();
    foreach (var knot in knotList)
    {
      knots.Add(knot);
    }

    // process weights
    // NOTE: if all weights are the same, autocad convention is to pass an empty list (this will assign them a value of -1)
    var weightsList = target.weights;
    if (target.closed && target.periodic) // handles closed periodic curves
    {
      weightsList = target.weights.GetRange(0, points.Count);
    }

    AG.DoubleCollection weights;
    weights =
      (weightsList.Distinct().Count() == 1)
        ? new AG.DoubleCollection()
        : new AG.DoubleCollection(weightsList.ToArray());

    AG.NurbCurve3d curve = new(target.degree, knots, pointCollection, weights, target.periodic);
    if (target.closed)
    {
      curve.MakeClosed();
    }

    curve.SetInterval(_intervalConverter.RawConvert(target.domain));

    return curve;
  }
}

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.AutocadShared.ToHost.Raw;
public class CurveToHostRawConverter : IRawConversion<SOG.Curve, AG.NurbCurve3d>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOP.Interval, AG.Interval> _intervalConverter;

  public CurveToHostRawConverter(IRawConversion<SOG.Point, AG.Point3d> pointConverter, IRawConversion<SOP.Interval, Interval> intervalConverter)
  {
    _pointConverter = pointConverter;
    _intervalConverter = intervalConverter;
  }

  public NurbCurve3d RawConvert(SOG.Curve target)
  {
    var points = target.GetPoints().Select(p => _pointConverter.RawConvert(p)).ToList();
    if (target.closed && target.periodic)
    {
      points = points.GetRange(0, points.Count - target.degree);
    }

    var pointCollection = new Point3dCollection(points.ToArray());

    // process knots
    // NOTE: Autocad defines spline knots  as a vector of size # control points + degree + 1. (# at start and end should match degree)
    // Conversions for autocad need to make sure this is satisfied, otherwise will cause protected mem crash.
    // NOTE: for **closed periodic** curves that have "n" control pts, # of knots should be n + 1. Remove degree = 3 knots from start and end.
    List<double> _knots = target.knots;
    if (target.knots.Count == points.Count + target.degree - 1) // handles rhino format curves
    {
      _knots.Insert(0, _knots[0]);
      _knots.Insert(_knots.Count - 1, _knots[_knots.Count - 1]);
    }
    if (target.closed && target.periodic) // handles closed periodic curves
    {
      _knots = _knots.GetRange(target.degree, _knots.Count - target.degree * 2);
    }

    var knots = new KnotCollection();
    foreach (var _knot in _knots)
    {
      knots.Add(_knot);
    }

    // process weights
    // NOTE: if all weights are the same, autocad convention is to pass an empty list (this will assign them a value of -1)
    var _weights = target.weights;
    if (target.closed && target.periodic) // handles closed periodic curves
    {
      _weights = target.weights.GetRange(0, points.Count);
    }

    DoubleCollection weights;
    weights = (_weights.Distinct().Count() == 1) ? new DoubleCollection() : new DoubleCollection(_weights.ToArray());

    NurbCurve3d _curve = new(target.degree, knots, pointCollection, weights, target.periodic);
    if (target.closed)
    {
      _curve.MakeClosed();
    }

    _curve.SetInterval(_intervalConverter.RawConvert(target.domain));

    return _curve;
  }
}

using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.NurbsCurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class NurbsCurveConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.NurbsCurve, SOG.Curve>
{
  private readonly IRawConversion<RG.Polyline, SOG.Polyline> _polylineConverter;
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public NurbsCurveConverter(
    IRawConversion<RG.Polyline, SOG.Polyline> polylineConverter,
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _polylineConverter = polylineConverter;
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.NurbsCurve)target);

  public SOG.Curve RawConvert(RG.NurbsCurve target)
  {
    target.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out var poly);
    SOG.Polyline displayValue = _polylineConverter.RawConvert(poly);

    var nurbsCurve = target.ToNurbsCurve();

    // increase knot multiplicity to (# control points + degree + 1)
    // add extra knots at start & end  because Rhino's knot multiplicity standard is (# control points + degree - 1)
    var knots = nurbsCurve.Knots.ToList();
    knots.Insert(0, knots[0]);
    knots.Insert(knots.Count - 1, knots[knots.Count - 1]);

    var myCurve = new SOG.Curve(displayValue, _contextStack.Current.SpeckleUnits)
    {
      weights = nurbsCurve.Points.Select(ctp => ctp.Weight).ToList(),
      points = nurbsCurve.Points
        .SelectMany(ctp => new[] { ctp.Location.X, ctp.Location.Y, ctp.Location.Z, ctp.Weight })
        .ToList(),
      knots = knots,
      degree = nurbsCurve.Degree,
      periodic = nurbsCurve.IsPeriodic,
      rational = nurbsCurve.IsRational,
      domain = _intervalConverter.RawConvert(nurbsCurve.Domain),
      closed = nurbsCurve.IsClosed,
      length = nurbsCurve.GetLength(),
      bbox = _boxConverter.RawConvert(new RG.Box(nurbsCurve.GetBoundingBox(true)))
    };

    return myCurve;
  }
}

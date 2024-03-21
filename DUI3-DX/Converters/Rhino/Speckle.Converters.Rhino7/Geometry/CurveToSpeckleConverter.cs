﻿using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Curve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Curve, ICurve>
{
  private readonly IRawConversion<RG.PolyCurve, SOG.Polycurve> _polyCurveConverter;
  private readonly IRawConversion<RG.Circle, SOG.Circle> _circleConverter;
  private readonly IRawConversion<RG.Arc, SOG.Arc> _arcConverter;
  private readonly IRawConversion<RG.Ellipse, SOG.Ellipse> _ellipseConverter;
  private readonly IRawConversion<RG.Polyline, SOG.Polyline> _polylineConverter;
  private readonly IRawConversion<RG.NurbsCurve, SOG.Curve> _nurbsCurveConverter;
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public CurveToSpeckleConverter(
    IRawConversion<RG.PolyCurve, SOG.Polycurve> polyCurveConverter,
    IRawConversion<RG.Circle, SOG.Circle> circleConverter,
    IRawConversion<RG.Arc, SOG.Arc> arcConverter,
    IRawConversion<RG.Ellipse, SOG.Ellipse> ellipseConverter,
    IRawConversion<RG.Polyline, SOG.Polyline> polylineConverter,
    IRawConversion<RG.NurbsCurve, SOG.Curve> nurbsCurveConverter,
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _polyCurveConverter = polyCurveConverter;
    _circleConverter = circleConverter;
    _arcConverter = arcConverter;
    _ellipseConverter = ellipseConverter;
    _polylineConverter = polylineConverter;
    _nurbsCurveConverter = nurbsCurveConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => (Base)RawConvert((RG.Curve)target); // POC: ICurve and Base are not related so we need to cast here.

  public ICurve RawConvert(RG.Curve target)
  {
    var tolerance = _contextStack.Current.Document.ModelAbsoluteTolerance;

    if (target is RG.PolyCurve polyCurve)
    {
      return _polyCurveConverter.RawConvert(polyCurve);
    }

    if (target.IsCircle(tolerance) && target.IsClosed)
    {
      if (target.TryGetCircle(out var getObj, tolerance))
      {
        var cir = _circleConverter.RawConvert(getObj);
        cir.domain = _intervalConverter.RawConvert(target.Domain);
        return cir;
      }
    }

    if (target.IsArc(tolerance))
    {
      if (target.TryGetArc(out var getObj, tolerance))
      {
        var arc = _arcConverter.RawConvert(getObj);
        arc.domain = _intervalConverter.RawConvert(target.Domain);
        return arc;
      }
    }

    if (target.IsEllipse(tolerance) && target.IsClosed)
    {
      target.TryGetPlane(out RG.Plane pln, tolerance);
      if (target.TryGetEllipse(pln, out var getObj, tolerance))
      {
        var ellipse = _ellipseConverter.RawConvert(getObj);
        ellipse.domain = _intervalConverter.RawConvert(target.Domain);
        return ellipse;
      }
    }

    if (target.IsLinear(tolerance) || target.IsPolyline())
    {
      if (target.TryGetPolyline(out var getObj))
      {
        var polyline = _polylineConverter.RawConvert(getObj);
        polyline.domain = _intervalConverter.RawConvert(target.Domain);
        return polyline;
      }
    }

    return _nurbsCurveConverter.RawConvert(target.ToNurbsCurve());
  }
}

using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class ICurveConverterToHost : ITypedConverter<ICurve, DB.CurveArray>
{
  private readonly ITypedConverter<SOG.Point, DB.XYZ> _pointConverter;
  private readonly ITypedConverter<SOG.Vector, DB.XYZ> _vectorConverter;
  private readonly ITypedConverter<SOG.Arc, DB.Arc> _arcConverter;
  private readonly ITypedConverter<SOG.Line, DB.Line> _lineConverter;
  private readonly ITypedConverter<SOG.Circle, DB.Arc> _circleConverter;
  private readonly ITypedConverter<SOG.Ellipse, DB.Curve> _ellipseConverter;
  private readonly ITypedConverter<SOG.Polyline, DB.CurveArray> _polylineConverter;
  private readonly ITypedConverter<SOG.Curve, DB.Curve> _curveConverter;

  public ICurveConverterToHost(
    ITypedConverter<SOG.Point, DB.XYZ> pointConverter,
    ITypedConverter<SOG.Vector, DB.XYZ> vectorConverter,
    ITypedConverter<SOG.Arc, DB.Arc> arcConverter,
    ITypedConverter<SOG.Line, DB.Line> lineConverter,
    ITypedConverter<SOG.Circle, DB.Arc> circleConverter,
    ITypedConverter<SOG.Ellipse, DB.Curve> ellipseConverter,
    ITypedConverter<SOG.Polyline, DB.CurveArray> polylineConverter,
    ITypedConverter<SOG.Curve, DB.Curve> curveConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
    _arcConverter = arcConverter;
    _lineConverter = lineConverter;
    _circleConverter = circleConverter;
    _ellipseConverter = ellipseConverter;
    _polylineConverter = polylineConverter;
    _curveConverter = curveConverter;
  }

  public DB.CurveArray Convert(ICurve target)
  {
    DB.CurveArray curveArray = new();
    switch (target)
    {
      case SOG.Line line:
        curveArray.Append(_lineConverter.Convert(line));
        return curveArray;

      case SOG.Arc arc:
        curveArray.Append(_arcConverter.Convert(arc));
        return curveArray;

      case SOG.Circle circle:
        curveArray.Append(_circleConverter.Convert(circle));
        return curveArray;

      case SOG.Ellipse ellipse:
        curveArray.Append(_ellipseConverter.Convert(ellipse));
        return curveArray;

      case SOG.Spiral spiral:
        return _polylineConverter.Convert(spiral.displayValue);

      case SOG.Curve nurbs:
        var n = _curveConverter.Convert(nurbs);

        // poc : in original converter, we were passing a bool into this method 'splitIfClosed'.
        // I'm not entirely sure why we need to split curves, but there are several occurances
        // of the method being called and overriding the bool to be true.

        //if (IsCurveClosed(n) && splitIfClosed)
        //{
        //  var split = SplitCurveInTwoHalves(n);
        //  curveArray.Append(split.Item1);
        //  curveArray.Append(split.Item2);
        //}
        //else
        //{
        //  curveArray.Append(n);
        //}
        curveArray.Append(n);
        return curveArray;

      case SOG.Polyline poly:
        return _polylineConverter.Convert(poly);

      case SOG.Polycurve plc:
        foreach (var seg in plc.segments)
        {
          // Enumerate all curves in the array to ensure polylines get fully converted.
          using var subCurves = Convert(seg);
          var crvEnumerator = subCurves.GetEnumerator();
          while (crvEnumerator.MoveNext() && crvEnumerator.Current != null)
          {
            curveArray.Append(crvEnumerator.Current as DB.Curve);
          }
        }
        return curveArray;
      default:
        throw new SpeckleConversionException($"The provided geometry of type {target.GetType()} is not a supported");
    }
  }

  public bool IsCurveClosed(DB.Curve nativeCurve, double tol = 1E-6)
  {
    var endPoint = nativeCurve.GetEndPoint(0);
    var source = nativeCurve.GetEndPoint(1);
    var distanceTo = endPoint.DistanceTo(source);
    return distanceTo < tol;
  }

  public (DB.Curve, DB.Curve) SplitCurveInTwoHalves(DB.Curve nativeCurve)
  {
    using var curveArray = new DB.CurveArray();
    // Revit does not like single curve loop edges, so we split them in two.
    var start = nativeCurve.GetEndParameter(0);
    var end = nativeCurve.GetEndParameter(1);
    var mid = start + ((end - start) / 2);

    var a = nativeCurve.Clone();
    a.MakeBound(start, mid);
    curveArray.Append(a);
    var b = nativeCurve.Clone();
    b.MakeBound(mid, end);
    curveArray.Append(b);

    return (a, b);
  }
}

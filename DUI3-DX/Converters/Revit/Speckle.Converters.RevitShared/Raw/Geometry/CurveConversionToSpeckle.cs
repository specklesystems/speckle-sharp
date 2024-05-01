using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class CurveConversionToSpeckle : IRawConversion<DB.Curve, ICurve>
{
  private readonly IRawConversion<DB.Line, SOG.Line> _lineConverter;
  private readonly IRawConversion<DB.Arc, SOG.Arc> _arcConverter;
  private readonly IRawConversion<DB.Arc, SOG.Circle> _circleConverter;
  private readonly IRawConversion<DB.Ellipse, SOG.Ellipse> _ellipseConverter;
  private readonly IRawConversion<DB.NurbSpline, SOG.Curve> _nurbsConverter;
  private readonly IRawConversion<DB.HermiteSpline, SOG.Curve> _hermiteConverter; // POC: should this be ICurve?

  public CurveConversionToSpeckle(
    IRawConversion<DB.Line, SOG.Line> lineConverter,
    IRawConversion<DB.Arc, SOG.Arc> arcConverter,
    IRawConversion<DB.Arc, SOG.Circle> circleConverter,
    IRawConversion<DB.Ellipse, SOG.Ellipse> ellipseConverter,
    IRawConversion<DB.NurbSpline, SOG.Curve> nurbsConverter,
    IRawConversion<DB.HermiteSpline, SOG.Curve> hermiteConverter
  )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _circleConverter = circleConverter;
    _ellipseConverter = ellipseConverter;
    _nurbsConverter = nurbsConverter;
    _hermiteConverter = hermiteConverter;
  }

  public ICurve RawConvert(DB.Curve target)
  {
    return target switch
    {
      DB.Line line => _lineConverter.RawConvert(line),
      // POC: are maybe arc.IsCyclic ?
      DB.Arc arc => arc.IsClosed ? _circleConverter.RawConvert(arc) : _arcConverter.RawConvert(arc),
      DB.Ellipse ellipse => _ellipseConverter.RawConvert(ellipse),
      DB.NurbSpline nurbs => _nurbsConverter.RawConvert(nurbs),
      DB.HermiteSpline hermite => _hermiteConverter.RawConvert(hermite),

      _ => throw new SpeckleConversionException($"Unsupported curve type {target.GetType()}")
    };
  }
}

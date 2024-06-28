using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class CurveConversionToSpeckle : ITypedConverter<DB.Curve, ICurve>
{
  private readonly ITypedConverter<DB.Line, SOG.Line> _lineConverter;
  private readonly ITypedConverter<DB.Arc, SOG.Arc> _arcConverter;
  private readonly ITypedConverter<DB.Arc, SOG.Circle> _circleConverter;
  private readonly ITypedConverter<DB.Ellipse, SOG.Ellipse> _ellipseConverter;
  private readonly ITypedConverter<DB.NurbSpline, SOG.Curve> _nurbsConverter;
  private readonly ITypedConverter<DB.HermiteSpline, SOG.Curve> _hermiteConverter; // POC: should this be ICurve?

  public CurveConversionToSpeckle(
    ITypedConverter<DB.Line, SOG.Line> lineConverter,
    ITypedConverter<DB.Arc, SOG.Arc> arcConverter,
    ITypedConverter<DB.Arc, SOG.Circle> circleConverter,
    ITypedConverter<DB.Ellipse, SOG.Ellipse> ellipseConverter,
    ITypedConverter<DB.NurbSpline, SOG.Curve> nurbsConverter,
    ITypedConverter<DB.HermiteSpline, SOG.Curve> hermiteConverter
  )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _circleConverter = circleConverter;
    _ellipseConverter = ellipseConverter;
    _nurbsConverter = nurbsConverter;
    _hermiteConverter = hermiteConverter;
  }

  public ICurve Convert(DB.Curve target)
  {
    return target switch
    {
      DB.Line line => _lineConverter.Convert(line),
      // POC: are maybe arc.IsCyclic ?
      DB.Arc arc => arc.IsClosed ? _circleConverter.Convert(arc) : _arcConverter.Convert(arc),
      DB.Ellipse ellipse => _ellipseConverter.Convert(ellipse),
      DB.NurbSpline nurbs => _nurbsConverter.Convert(nurbs),
      DB.HermiteSpline hermite => _hermiteConverter.Convert(hermite),

      _ => throw new SpeckleConversionException($"Unsupported curve type {target.GetType()}")
    };
  }
}

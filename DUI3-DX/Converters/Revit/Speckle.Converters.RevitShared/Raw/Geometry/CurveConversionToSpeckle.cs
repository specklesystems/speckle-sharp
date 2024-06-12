using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class CurveConversionToSpeckle : ITypedConverter<IRevitCurve, ICurve>
{
  private readonly ITypedConverter<IRevitLine, SOG.Line> _lineConverter;
  private readonly ITypedConverter<IRevitArc, SOG.Arc> _arcConverter;
  private readonly ITypedConverter<IRevitArc, SOG.Circle> _circleConverter;
  private readonly ITypedConverter<IRevitEllipse, SOG.Ellipse> _ellipseConverter;
  private readonly ITypedConverter<IRevitNurbSpline, SOG.Curve> _nurbsConverter;
  private readonly ITypedConverter<IRevitHermiteSpline, SOG.Curve> _hermiteConverter; // POC: should this be ICurve?

  public CurveConversionToSpeckle(
    ITypedConverter<IRevitLine, SOG.Line> lineConverter,
    ITypedConverter<IRevitArc, SOG.Arc> arcConverter,
    ITypedConverter<IRevitArc, SOG.Circle> circleConverter,
    ITypedConverter<IRevitEllipse, SOG.Ellipse> ellipseConverter,
    ITypedConverter<IRevitNurbSpline, SOG.Curve> nurbsConverter,
    ITypedConverter<IRevitHermiteSpline, SOG.Curve> hermiteConverter
  )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _circleConverter = circleConverter;
    _ellipseConverter = ellipseConverter;
    _nurbsConverter = nurbsConverter;
    _hermiteConverter = hermiteConverter;
  }

  public ICurve Convert(IRevitCurve target)
  {
    return target switch
    {
      IRevitLine line => _lineConverter.Convert(line),
      // POC: are maybe arc.IsCyclic ?
      IRevitArc arc => arc.IsClosed ? _circleConverter.Convert(arc) : _arcConverter.Convert(arc),
      IRevitEllipse ellipse => _ellipseConverter.Convert(ellipse),
      IRevitNurbSpline nurbs => _nurbsConverter.Convert(nurbs),
      IRevitHermiteSpline hermite => _hermiteConverter.Convert(hermite),

      _ => throw new SpeckleConversionException($"Unsupported curve type {target.GetType()}")
    };
  }
}

using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class CurveConversionToSpeckle : IRawConversion<DB.Curve, ICurve>
{
  // POC: can we do this sort of thing?
  // Can this converter be made generic to make a ConverterFetcher? and be used
  // whenever we have some ambiguity as to the specific converter we need to call?
  // IIndex<string, IRawConversion<DB.Curve, ICurve>> _curveConverters;

  private readonly IRawConversion<DB.Line, SOG.Line> _lineConverter;
  private readonly IRawConversion<DB.Arc, SOG.Arc> _arcConverter;
  private readonly IRawConversion<DB.Ellipse, SOG.Ellipse> _ellipseConverter;
  private readonly IRawConversion<DB.NurbSpline, SOG.Curve> _nurbsConverter;

  //private readonly IRawConversion<DB.HermiteSpline, SOG.Line> _hermiteConverter;

  public CurveConversionToSpeckle(
    IRawConversion<DB.Line, SOG.Line> lineConverter,
    IRawConversion<DB.Arc, SOG.Arc> arcConverter,
    IRawConversion<DB.Ellipse, SOG.Ellipse> ellipseConverter
  )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _ellipseConverter = ellipseConverter;
  }

  public ICurve RawConvert(DB.Curve target)
  {
    return target switch
    {
      DB.Line line => _lineConverter.RawConvert(line),
      DB.Arc arc => _arcConverter.RawConvert(arc),
      DB.Ellipse ellipse => _ellipseConverter.RawConvert(ellipse),
      DB.NurbSpline nurbs => _nurbsConverter.RawConvert(nurbs),
      //DB.HermiteSpline hermite => _hermiteConverter.RawConvert(hermite),
      _ => throw new SpeckleConversionException($"Unsupported curve type {target.GetType()}")
    };
  }
}

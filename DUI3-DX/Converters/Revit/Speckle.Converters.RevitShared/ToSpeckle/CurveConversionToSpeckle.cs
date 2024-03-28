using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Curve), 0)]
public class CurveConversionToSpeckle : BaseConversionToSpeckle<DB.Curve, ICurve>
{
  private readonly IRawConversion<DB.Line, SOG.Line> _lineConverter;

  //private readonly IRawConversion<DB.Arc, SOG.Line> _arcConverter;
  //private readonly IRawConversion<DB.Ellipse, SOG.Line> _ellipseConverter;
  //private readonly IRawConversion<DB.NurbSpline, SOG.Line> _nurbsConverter;
  //private readonly IRawConversion<DB.HermiteSpline, SOG.Line> _hermiteConverter;

  public CurveConversionToSpeckle(IRawConversion<DB.Line, SOG.Line> lineConverter)
  {
    _lineConverter = lineConverter;
  }

  public override ICurve RawConvert(DB.Curve target)
  {
    return target switch
    {
      DB.Line line => _lineConverter.RawConvert(line),
      //DB.Arc arc => _arcConverter.RawConvert(arc),
      //DB.Ellipse ellipse => _ellipseConverter.RawConvert(ellipse),
      //DB.NurbSpline nurbs => _nurbsConverter.RawConvert(nurbs),
      //DB.HermiteSpline hermite => _hermiteConverter.RawConvert(hermite),
      _ => throw new SpeckleConversionException($"Unsupported curve type {target.GetType()}")
    };
  }
}

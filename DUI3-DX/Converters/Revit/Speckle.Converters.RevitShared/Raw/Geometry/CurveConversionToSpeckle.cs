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

  //private readonly IRawConversion<DB.Arc, SOG.Line> _arcConverter;
  //private readonly IRawConversion<DB.Ellipse, SOG.Line> _ellipseConverter;
  //private readonly IRawConversion<DB.NurbSpline, SOG.Line> _nurbsConverter;
  //private readonly IRawConversion<DB.HermiteSpline, SOG.Line> _hermiteConverter;

  public CurveConversionToSpeckle(IRawConversion<DB.Line, SOG.Line> lineConverter)
  {
    _lineConverter = lineConverter;
  }

  public ICurve RawConvert(DB.Curve target)
  {
    // POC: and then here:
    // if (_curveConverters.TryGetValue(target.GetType().Name, out IRawConversion<DB.Curve, ICurve> converter))
    // {
    //   return converter.RawConvert(target);
    // }
    //
    // throw ...

    return target switch
    {
      DB.Line line => _lineConverter.RawConvert(line),
      // POC: these conversions are "coming soon" can we use IIndex with variance with nice injection
      //DB.Arc arc => _arcConverter.RawConvert(arc),
      //DB.Ellipse ellipse => _ellipseConverter.RawConvert(ellipse),
      //DB.NurbSpline nurbs => _nurbsConverter.RawConvert(nurbs),
      //DB.HermiteSpline hermite => _hermiteConverter.RawConvert(hermite),
      _ => throw new SpeckleConversionException($"Unsupported curve type {target.GetType()}")
    };
  }
}

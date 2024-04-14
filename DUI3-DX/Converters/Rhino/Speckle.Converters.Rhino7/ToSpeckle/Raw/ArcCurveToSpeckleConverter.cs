using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ArcCurveToSpeckleConverter : IRawConversion<RG.ArcCurve, ICurve>, IRawConversion<RG.ArcCurve, Base>
{
  private readonly IRawConversion<RG.Circle, SOG.Circle> _circleConverter;
  private readonly IRawConversion<RG.Arc, SOG.Arc> _arcConverter;
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public ArcCurveToSpeckleConverter(
    IRawConversion<RG.Circle, SOG.Circle> circleConverter,
    IRawConversion<RG.Arc, SOG.Arc> arcConverter,
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _circleConverter = circleConverter;
    _arcConverter = arcConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  public ICurve RawConvert(RG.ArcCurve target)
  {
    var tolerance = _contextStack.Current.Document.ModelAbsoluteTolerance;

    if (target.IsCompleteCircle)
    {
      target.TryGetCircle(out var getObj, tolerance);
      var cir = _circleConverter.RawConvert(getObj);
      cir.domain = _intervalConverter.RawConvert(target.Domain);
      return cir;
    }

    var arc = _arcConverter.RawConvert(target.Arc);
    arc.domain = _intervalConverter.RawConvert(target.Domain);
    return arc;
  }

  // POC: Need to implement this because ICurve and Base are not related, this one is needed at the top-level, the other is for better typed experience.
  //      This also causes us to have to force cast ICurve to Base as a result, which is expected to always succeed but not nice.
  Base IRawConversion<RG.ArcCurve, Base>.RawConvert(RG.ArcCurve target) => (Base)RawConvert(target);
}

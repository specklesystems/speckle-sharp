using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ArcCurveToSpeckleConverter : ITypedConverter<RG.ArcCurve, ICurve>, ITypedConverter<RG.ArcCurve, Base>
{
  private readonly ITypedConverter<RG.Circle, SOG.Circle> _circleConverter;
  private readonly ITypedConverter<RG.Arc, SOG.Arc> _arcConverter;
  private readonly ITypedConverter<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public ArcCurveToSpeckleConverter(
    ITypedConverter<RG.Circle, SOG.Circle> circleConverter,
    ITypedConverter<RG.Arc, SOG.Arc> arcConverter,
    ITypedConverter<RG.Interval, SOP.Interval> intervalConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _circleConverter = circleConverter;
    _arcConverter = arcConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts an RG.ArcCurve to an ICurve.
  /// </summary>
  /// <param name="target">The RG.ArcCurve to convert.</param>
  /// <returns>The converted ICurve.</returns>
  /// <remarks>
  /// ⚠️ If the provided ArcCurve is a complete circle, a Speckle Circle will be returned.
  /// Otherwise, the output will be a Speckle Arc. <br/>
  /// ✅ This method preserves the domain of the original ArcCurve.<br/>
  /// </remarks>
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

  // POC: CNX-9275 Need to implement this because ICurve and Base are not related, this one is needed at the top-level, the other is for better typed experience.
  //      This also causes us to have to force cast ICurve to Base as a result, which is expected to always succeed but not nice.
  /// <inheritdoc cref="RawConvert"/>
  /// <returns> The converted ICurve with a cast to <see cref="Base"/> object</returns>
  Base ITypedConverter<RG.ArcCurve, Base>.RawConvert(RG.ArcCurve target) => (Base)RawConvert(target);
}

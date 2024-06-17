using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ArcCurveToSpeckleConverter : ITypedConverter<IRhinoArcCurve, ICurve>, ITypedConverter<IRhinoArcCurve, Base>
{
  private readonly ITypedConverter<IRhinoCircle, SOG.Circle> _circleConverter;
  private readonly ITypedConverter<IRhinoArc, SOG.Arc> _arcConverter;
  private readonly ITypedConverter<IRhinoInterval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public ArcCurveToSpeckleConverter(
    ITypedConverter<IRhinoCircle, SOG.Circle> circleConverter,
    ITypedConverter<IRhinoArc, SOG.Arc> arcConverter,
    ITypedConverter<IRhinoInterval, SOP.Interval> intervalConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _circleConverter = circleConverter;
    _arcConverter = arcConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts an IRhinoArcCurve to an ICurve.
  /// </summary>
  /// <param name="target">The IRhinoArcCurve to convert.</param>
  /// <returns>The converted ICurve.</returns>
  /// <remarks>
  /// ⚠️ If the provided ArcCurve is a complete circle, a Speckle Circle will be returned.
  /// Otherwise, the output will be a Speckle Arc. <br/>
  /// ✅ This method preserves the domain of the original ArcCurve.<br/>
  /// </remarks>
  public ICurve Convert(IRhinoArcCurve target)
  {
    var tolerance = _contextStack.Current.Document.ModelAbsoluteTolerance;

    if (target.IsCompleteCircle)
    {
      target.TryGetCircle(out var getObj, tolerance);
      var cir = _circleConverter.Convert(getObj);
      cir.domain = _intervalConverter.Convert(target.Domain);
      return cir;
    }

    var arc = _arcConverter.Convert(target.Arc);
    arc.domain = _intervalConverter.Convert(target.Domain);
    return arc;
  }

  // POC: CNX-9275 Need to implement this because ICurve and Base are not related, this one is needed at the top-level, the other is for better typed experience.
  //      This also causes us to have to force cast ICurve to Base as a result, which is expected to always succeed but not nice.
  /// <inheritdoc cref="Convert"/>
  /// <returns> The converted ICurve with a cast to <see cref="Base"/> object</returns>
  Base ITypedConverter<IRhinoArcCurve, Base>.Convert(IRhinoArcCurve target) => (Base)Convert(target);
}

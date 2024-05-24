using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

/// <summary>
/// Converts a SpeckleArcRaw object to a Rhino.Geometry.Arc object or Rhino.Geometry.ArcCurve object.
/// </summary>
public class ArcToHostConverter : ITypedConverter<SOG.Arc, RG.Arc>, ITypedConverter<SOG.Arc, RG.ArcCurve>
{
  private readonly ITypedConverter<SOG.Point, RG.Point3d> _pointConverter;
  private readonly ITypedConverter<SOP.Interval, RG.Interval> _intervalConverter;

  public ArcToHostConverter(
    ITypedConverter<SOG.Point, RG.Point3d> pointConverter,
    ITypedConverter<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _pointConverter = pointConverter;
    this._intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts a <see cref="SOG.Arc"/> object to a <see cref="RG.Arc"/> object.
  /// </summary>
  /// <param name="target">The Speckle Arc object to convert.</param>
  /// <returns>The converted <see cref="RG.Arc"/> object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ This method does not preserve the original curve domain</remarks>
  public RG.Arc Convert(SOG.Arc target)
  {
    var rhinoArc = new RG.Arc(
      _pointConverter.Convert(target.startPoint),
      _pointConverter.Convert(target.midPoint),
      _pointConverter.Convert(target.endPoint)
    );
    return rhinoArc;
  }

  // POC: CNX-9271 Potential code-smell by directly implementing the interface. We should discuss this further but
  // since we're using the interfaces instead of the direct type, this may not be an issue.
  /// <summary>
  /// Converts a <see cref="SOG.Arc"/> object to a <see cref="RG.ArcCurve"/> object.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Arc"/> object to convert.</param>
  /// <returns>The converted <see cref="RG.ArcCurve"/> object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ Converting to <see cref="RG.ArcCurve"/> instead of <see cref="RG.Arc"/> preserves the domain of the curve.</remarks>
  RG.ArcCurve ITypedConverter<SOG.Arc, RG.ArcCurve>.Convert(SOG.Arc target)
  {
    var rhinoArc = Convert(target);
    var arcCurve = new RG.ArcCurve(rhinoArc) { Domain = _intervalConverter.Convert(target.domain) };
    return arcCurve;
  }
}

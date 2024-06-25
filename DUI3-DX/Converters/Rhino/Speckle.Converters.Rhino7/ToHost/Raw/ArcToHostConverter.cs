using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

/// <summary>
/// Converts a SpeckleArcRaw object to a Rhino.Geometry.Arc object or Rhino.Geometry.ArcCurve object.
/// </summary>
public class ArcToHostConverter : ITypedConverter<SOG.Arc, IRhinoArc>, ITypedConverter<SOG.Arc, IRhinoArcCurve>
{
  private readonly ITypedConverter<SOG.Point, IRhinoPoint3d> _pointConverter;
  private readonly ITypedConverter<SOP.Interval, IRhinoInterval> _intervalConverter;
  private readonly IRhinoArcFactory _rhinoArcFactory;

  public ArcToHostConverter(
    ITypedConverter<SOG.Point, IRhinoPoint3d> pointConverter,
    ITypedConverter<SOP.Interval, IRhinoInterval> intervalConverter,
    IRhinoArcFactory rhinoArcFactory
  )
  {
    _pointConverter = pointConverter;
    this._intervalConverter = intervalConverter;
    _rhinoArcFactory = rhinoArcFactory;
  }

  /// <summary>
  /// Converts a <see cref="SOG.Arc"/> object to a <see cref="IRhinoArc"/> object.
  /// </summary>
  /// <param name="target">The Speckle Arc object to convert.</param>
  /// <returns>The converted <see cref="IRhinoArc"/> object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ This method does not preserve the original curve domain</remarks>
  public IRhinoArc Convert(SOG.Arc target)
  {
    var rhinoArc = _rhinoArcFactory.Create(
      _pointConverter.Convert(target.startPoint),
      _pointConverter.Convert(target.midPoint),
      _pointConverter.Convert(target.endPoint)
    );
    return rhinoArc;
  }

  // POC: CNX-9271 Potential code-smell by directly implementing the interface. We should discuss this further but
  // since we're using the interfaces instead of the direct type, this may not be an issue.
  /// <summary>
  /// Converts a <see cref="SOG.Arc"/> object to a <see cref="IRhinoArcCurve"/> object.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Arc"/> object to convert.</param>
  /// <returns>The converted <see cref="IRhinoArcCurve"/> object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ Converting to <see cref="IRhinoArcCurve"/> instead of <see cref="IRhinoArc"/> preserves the domain of the curve.</remarks>
  IRhinoArcCurve ITypedConverter<SOG.Arc, IRhinoArcCurve>.Convert(SOG.Arc target)
  {
    var rhinoArc = Convert(target);
    var arcCurve = _rhinoArcFactory.Create(rhinoArc, _intervalConverter.Convert(target.domain));
    return arcCurve;
  }
}

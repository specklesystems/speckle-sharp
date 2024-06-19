using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class LineToHostConverter : ITypedConverter<SOG.Line, IRhinoLineCurve>, ITypedConverter<SOG.Line, IRhinoLine>
{
  private readonly ITypedConverter<SOG.Point, IRhinoPoint3d> _pointConverter;
  private readonly IRhinoLineFactory _rhinoLineFactory;

  public LineToHostConverter(
    ITypedConverter<SOG.Point, IRhinoPoint3d> pointConverter,
    IRhinoLineFactory rhinoLineFactory
  )
  {
    _pointConverter = pointConverter;
    _rhinoLineFactory = rhinoLineFactory;
  }

  /// <summary>
  /// Converts a Speckle Line object to a Rhino Line object.
  /// </summary>
  /// <param name="target">The Speckle Line object to convert.</param>
  /// <returns>Returns the resulting Rhino Line object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks>
  /// <br/>⚠️ This conversion does not preserve the curve domain.
  /// If you need it preserved you must request a conversion to
  /// <see cref="IRhinoLineCurve"/> conversion instead
  /// </remarks>
  public IRhinoLine Convert(SOG.Line target) =>
    _rhinoLineFactory.Create(_pointConverter.Convert(target.start), _pointConverter.Convert(target.end));

  /// <summary>
  /// Converts a Speckle Line object to a Rhino LineCurve object.
  /// </summary>
  /// <param name="target">The Speckle Line object to convert.</param>
  /// <returns>Returns the resulting Rhino LineCurve object.</returns>
  IRhinoLineCurve ITypedConverter<SOG.Line, IRhinoLineCurve>.Convert(SOG.Line target) =>
    _rhinoLineFactory.Create(Convert(target));
}

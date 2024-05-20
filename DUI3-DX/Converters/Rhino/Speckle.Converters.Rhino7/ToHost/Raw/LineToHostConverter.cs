using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class LineToHostConverter : ITypedConverter<SOG.Line, RG.LineCurve>, ITypedConverter<SOG.Line, RG.Line>
{
  private readonly ITypedConverter<SOG.Point, RG.Point3d> _pointConverter;

  public LineToHostConverter(ITypedConverter<SOG.Point, RG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
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
  /// <see cref="RG.LineCurve"/> conversion instead
  /// </remarks>
  public RG.Line RawConvert(SOG.Line target) =>
    new(_pointConverter.RawConvert(target.start), _pointConverter.RawConvert(target.end));

  /// <summary>
  /// Converts a Speckle Line object to a Rhino LineCurve object.
  /// </summary>
  /// <param name="target">The Speckle Line object to convert.</param>
  /// <returns>Returns the resulting Rhino LineCurve object.</returns>
  RG.LineCurve ITypedConverter<SOG.Line, RG.LineCurve>.RawConvert(SOG.Line target) => new(RawConvert(target));
}

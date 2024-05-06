using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

/// <summary>
/// This class is responsible for converting a <see cref="SOG.Circle"/> into <see cref="RG.Circle"/> and <see cref="RG.ArcCurve"/> objects.
/// Implements the <see cref="IRawConversion{TIn, TOut}"/> interface,
/// providing implementation for <see cref="SOG.Circle"/> to <see cref="RG.Circle"/> and <see cref="RG.ArcCurve"/> conversion.
/// </summary>
public class CircleToHostConverter : IRawConversion<SOG.Circle, RG.Circle>, IRawConversion<SOG.Circle, RG.ArcCurve>
{
  private readonly IRawConversion<SOG.Plane, RG.Plane> _planeConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  /// <summary>
  /// Constructs a new instance of the <see cref="CircleToHostConverter"/> class.
  /// </summary>
  /// <param name="intervalConverter">
  /// An implementation of <see cref="IRawConversion{TIn, TOut}"/> used to convert <see cref="SOP.Interval"/> into <see cref="RG.Interval"/>.
  /// </param>
  /// <param name="planeConverter">
  /// An implementation of <see cref="IRawConversion{TIn, TOut}"/> used to convert <see cref="SOG.Plane"/> into <see cref="RG.Plane"/>.
  /// </param>
  public CircleToHostConverter(
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter,
    IRawConversion<SOG.Plane, RG.Plane> planeConverter
  )
  {
    _intervalConverter = intervalConverter;
    _planeConverter = planeConverter;
  }

  /// <summary>
  /// Converts the given <see cref="SOG.Circle"/> object into a <see cref="RG.Circle"/> object.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Circle"/> object to convert.</param>
  /// <returns>The resulting <see cref="RG.Circle"/> object.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when the radius of the given <see cref="SOG.Circle"/> object is null.
  /// </exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ This conversion does not preserve the curve domain. If you need it preserved you must request a conversion to <see cref="RG.ArcCurve"/> conversion instead</remarks>
  public RG.Circle RawConvert(SOG.Circle target)
  {
    if (target.radius == null)
    {
      // POC: CNX-9272 Circle radius being nullable makes no sense
      throw new ArgumentNullException(nameof(target), "Circle radius cannot be null");
    }

    var plane = _planeConverter.RawConvert(target.plane);
    var radius = target.radius.Value;
    return new RG.Circle(plane, radius);
  }

  RG.ArcCurve IRawConversion<SOG.Circle, RG.ArcCurve>.RawConvert(SOG.Circle target) =>
    new(RawConvert(target)) { Domain = _intervalConverter.RawConvert(target.domain) };
}

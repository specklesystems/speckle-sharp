using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

/// <summary>
/// This class is responsible for converting a <see cref="SOG.Circle"/> into <see cref="IRhinoCircle"/> and <see cref="IRhinoArcCurve"/> objects.
/// Implements the <see cref="ITypedConverter{TIn,TOut}"/> interface,
/// providing implementation for <see cref="SOG.Circle"/> to <see cref="IRhinoCircle"/> and <see cref="IRhinoArcCurve"/> conversion.
/// </summary>
public class CircleToHostConverter : ITypedConverter<SOG.Circle, IRhinoCircle>, ITypedConverter<SOG.Circle, IRhinoArcCurve>
{
  private readonly ITypedConverter<SOG.Plane, IRhinoPlane> _planeConverter;
  private readonly ITypedConverter<SOP.Interval, IRhinoInterval> _intervalConverter;
  private readonly IRhinoCircleFactory _rhinoCircleFactory;

  /// <summary>
  /// Constructs a new instance of the <see cref="CircleToHostConverter"/> class.
  /// </summary>
  /// <param name="intervalConverter">
  /// An implementation of <see cref="ITypedConverter{TIn,TOut}"/> used to convert <see cref="SOP.Interval"/> into <see cref="IRhinoInterval"/>.
  /// </param>
  /// <param name="planeConverter">
  /// An implementation of <see cref="ITypedConverter{TIn,TOut}"/> used to convert <see cref="SOG.Plane"/> into <see cref="IRhinoPlane"/>.
  /// </param>
  public CircleToHostConverter(
    ITypedConverter<SOP.Interval, IRhinoInterval> intervalConverter,
    ITypedConverter<SOG.Plane, IRhinoPlane> planeConverter, IRhinoCircleFactory rhinoCircleFactory)
  {
    _intervalConverter = intervalConverter;
    _planeConverter = planeConverter;
    _rhinoCircleFactory = rhinoCircleFactory;
  }

  /// <summary>
  /// Converts the given <see cref="SOG.Circle"/> object into a <see cref="IRhinoCircle"/> object.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Circle"/> object to convert.</param>
  /// <returns>The resulting <see cref="IRhinoCircle"/> object.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when the radius of the given <see cref="SOG.Circle"/> object is null.
  /// </exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ This conversion does not preserve the curve domain. If you need it preserved you must request a conversion to <see cref="IRhinoArcCurve"/> conversion instead</remarks>
  public IRhinoCircle Convert(SOG.Circle target)
  {
    if (target.radius == null)
    {
      // POC: CNX-9272 Circle radius being nullable makes no sense
      throw new ArgumentNullException(nameof(target), "Circle radius cannot be null");
    }

    var plane = _planeConverter.Convert(target.plane);
    var radius = target.radius.Value;
    return _rhinoCircleFactory.Create(plane, radius);
  }

  IRhinoArcCurve ITypedConverter<SOG.Circle, IRhinoArcCurve>.Convert(SOG.Circle target) =>
    _rhinoCircleFactory.Create(Convert(target), _intervalConverter.Convert(target.domain));
}

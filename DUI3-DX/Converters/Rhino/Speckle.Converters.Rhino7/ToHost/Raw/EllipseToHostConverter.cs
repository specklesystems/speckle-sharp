using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class EllipseToHostConverter
  : ITypedConverter<SOG.Ellipse, IRhinoEllipse>,
    ITypedConverter<SOG.Ellipse, IRhinoNurbsCurve>
{
  private readonly ITypedConverter<SOG.Plane, IRhinoPlane> _planeConverter;
  private readonly ITypedConverter<SOP.Interval, IRhinoInterval> _intervalConverter;
  private readonly IRhinoEllipseFactory _rhinoEllipseFactory;

  public EllipseToHostConverter(
    ITypedConverter<SOG.Plane, IRhinoPlane> planeConverter,
    ITypedConverter<SOP.Interval, IRhinoInterval> intervalConverter, IRhinoEllipseFactory rhinoEllipseFactory)
  {
    _planeConverter = planeConverter;
    _intervalConverter = intervalConverter;
    _rhinoEllipseFactory = rhinoEllipseFactory;
  }

  /// <summary>
  /// Converts an instance of <see cref="SOG.Ellipse"/> to an <see cref="IRhinoEllipse"/> while preserving geometric properties.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Ellipse"/> instance to be converted.</param>
  /// <returns>The resulting <see cref="IRhinoEllipse"/> after conversion.</returns>
  /// <exception cref="InvalidOperationException">Thrown when <see cref="SOG.Ellipse.firstRadius"/> or <see cref="SOG.Ellipse.secondRadius"/> properties are null.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ This conversion does not preserve the curve domain. If you need it preserved you must request a conversion to <see cref="IRhinoNurbsCurve"/> conversion instead</remarks>
  public IRhinoEllipse Convert(SOG.Ellipse target)
  {
    if (!target.firstRadius.HasValue || !target.secondRadius.HasValue)
    {
      throw new InvalidOperationException($"Ellipses cannot have null radii");
    }

    return _rhinoEllipseFactory.Create(_planeConverter.Convert(target.plane), target.firstRadius.Value, target.secondRadius.Value);
  }

  /// <summary>
  /// Converts the provided <see cref="SOG.Ellipse"/> into a <see cref="IRhinoNurbsCurve"/> representation.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Ellipse"/> to convert.</param>
  /// <returns>
  /// A <see cref="IRhinoNurbsCurve"/> that represents the provided <see cref="SOG.Ellipse"/>.
  /// </returns>
  IRhinoNurbsCurve ITypedConverter<SOG.Ellipse, IRhinoNurbsCurve>.Convert(SOG.Ellipse target)
  {
    var rhinoEllipse = Convert(target);
    var rhinoNurbsEllipse = rhinoEllipse.ToNurbsCurve();
    rhinoNurbsEllipse.Domain = _intervalConverter.Convert(target.domain);

    if (target.trimDomain != null)
    {
      rhinoNurbsEllipse = rhinoNurbsEllipse.Trim(_intervalConverter.Convert(target.trimDomain)).ToNurbsCurve();
    }

    return rhinoNurbsEllipse;
  }
}

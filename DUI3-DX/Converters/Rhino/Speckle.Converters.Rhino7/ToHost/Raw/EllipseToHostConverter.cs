using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class EllipseToHostConverter
  : ITypedConverter<SOG.Ellipse, RG.Ellipse>,
    ITypedConverter<SOG.Ellipse, RG.NurbsCurve>
{
  private readonly ITypedConverter<SOG.Plane, RG.Plane> _planeConverter;
  private readonly ITypedConverter<SOP.Interval, RG.Interval> _intervalConverter;

  public EllipseToHostConverter(
    ITypedConverter<SOG.Plane, RG.Plane> planeConverter,
    ITypedConverter<SOP.Interval, RG.Interval> intervalConverter
  )
  {
    _planeConverter = planeConverter;
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts an instance of <see cref="SOG.Ellipse"/> to an <see cref="RG.Ellipse"/> while preserving geometric properties.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Ellipse"/> instance to be converted.</param>
  /// <returns>The resulting <see cref="RG.Ellipse"/> after conversion.</returns>
  /// <exception cref="InvalidOperationException">Thrown when <see cref="SOG.Ellipse.firstRadius"/> or <see cref="SOG.Ellipse.secondRadius"/> properties are null.</exception>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  /// <remarks><br/>⚠️ This conversion does not preserve the curve domain. If you need it preserved you must request a conversion to <see cref="RG.NurbsCurve"/> conversion instead</remarks>
  public RG.Ellipse Convert(SOG.Ellipse target)
  {
    if (!target.firstRadius.HasValue || !target.secondRadius.HasValue)
    {
      throw new InvalidOperationException($"Ellipses cannot have null radii");
    }

    return new RG.Ellipse(
      _planeConverter.Convert(target.plane),
      target.firstRadius.Value,
      target.secondRadius.Value
    );
  }

  /// <summary>
  /// Converts the provided <see cref="SOG.Ellipse"/> into a <see cref="RG.NurbsCurve"/> representation.
  /// </summary>
  /// <param name="target">The <see cref="SOG.Ellipse"/> to convert.</param>
  /// <returns>
  /// A <see cref="RG.NurbsCurve"/> that represents the provided <see cref="SOG.Ellipse"/>.
  /// </returns>
  RG.NurbsCurve ITypedConverter<SOG.Ellipse, RG.NurbsCurve>.Convert(SOG.Ellipse target)
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

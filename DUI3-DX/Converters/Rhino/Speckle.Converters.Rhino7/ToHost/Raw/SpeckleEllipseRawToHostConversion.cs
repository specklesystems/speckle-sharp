using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleEllipseRawToHostConversion
  : IRawConversion<SOG.Ellipse, RG.Ellipse>,
    IRawConversion<SOG.Ellipse, RG.NurbsCurve>
{
  private readonly IRawConversion<SOG.Plane, RG.Plane> _planeConverter;
  private readonly IRawConversion<SOP.Interval, RG.Interval> _intervalConverter;

  public SpeckleEllipseRawToHostConversion(
    IRawConversion<SOG.Plane, RG.Plane> planeConverter,
    IRawConversion<SOP.Interval, RG.Interval> intervalConverter
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
  public RG.Ellipse RawConvert(SOG.Ellipse target)
  {
    if (!target.firstRadius.HasValue || !target.secondRadius.HasValue)
    {
      throw new InvalidOperationException($"Ellipses cannot have null radii");
    }

    return new RG.Ellipse(
      _planeConverter.RawConvert(target.plane),
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
  RG.NurbsCurve IRawConversion<SOG.Ellipse, RG.NurbsCurve>.RawConvert(SOG.Ellipse target)
  {
    var rhinoEllipse = RawConvert(target);
    var rhinoNurbsEllipse = rhinoEllipse.ToNurbsCurve();
    rhinoNurbsEllipse.Domain = _intervalConverter.RawConvert(target.domain);

    if (target.trimDomain != null)
    {
      rhinoNurbsEllipse = rhinoNurbsEllipse.Trim(_intervalConverter.RawConvert(target.trimDomain)).ToNurbsCurve();
    }

    return rhinoNurbsEllipse;
  }
}

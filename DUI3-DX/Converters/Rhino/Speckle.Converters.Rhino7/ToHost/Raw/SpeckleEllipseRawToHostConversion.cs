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

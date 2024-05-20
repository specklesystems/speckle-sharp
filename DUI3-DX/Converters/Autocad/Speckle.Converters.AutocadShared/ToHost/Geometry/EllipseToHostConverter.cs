using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class EllipseToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Ellipse, ADB.Ellipse>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;
  private readonly ITypedConverter<SOG.Vector, AG.Vector3d> _vectorConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public EllipseToHostConverter(
    ITypedConverter<SOG.Point, AG.Point3d> pointConverter,
    ITypedConverter<SOG.Vector, AG.Vector3d> vectorConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Ellipse)target);

  /// <exception cref="ArgumentNullException"> Throws if any ellipse radius value is null.</exception>
  public ADB.Ellipse RawConvert(SOG.Ellipse target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    AG.Point3d origin = _pointConverter.RawConvert(target.plane.origin);
    AG.Vector3d normal = _vectorConverter.RawConvert(target.plane.normal);
    AG.Vector3d xAxis = _vectorConverter.RawConvert(target.plane.xdir);

    // POC: how possibly we might have firstRadius and secondRadius is possibly null?
    if (target.firstRadius is null || target.secondRadius is null)
    {
      throw new ArgumentNullException(nameof(target), "Cannot convert ellipse without radius values.");
    }

    AG.Vector3d majorAxis = f * (double)target.firstRadius * xAxis.GetNormal();
    double radiusRatio = (double)target.secondRadius / (double)target.firstRadius;

    // get trim
    double startAngle = 0;
    double endAngle = Math.PI * 2;
    if (
      target.domain.start is double domainStart
      && target.domain.end is double domainEnd
      && target.trimDomain is SOP.Interval trim
      && trim.start is double start
      && trim.end is double end
    )
    {
      // normalize the start and end trim values to [0,2pi]
      startAngle = (start - domainStart) / (domainEnd - domainStart) * Math.PI * 2;
      endAngle = (end - domainStart) / (domainEnd - domainStart) * Math.PI * 2;
    }

    ADB.Ellipse ellipse = new(origin, normal, majorAxis, radiusRatio, startAngle, endAngle);

    return ellipse;
  }
}

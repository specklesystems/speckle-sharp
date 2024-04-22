using System;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class EllipseToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Ellipse, ADB.Ellipse>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public EllipseToHostConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<SOG.Vector, AG.Vector3d> vectorConverter,
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
    return new(origin, normal, majorAxis, radiusRatio, 0, 2 * Math.PI);
  }
}

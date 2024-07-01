using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Circle, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public CircleToHostConverter(
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Circle)target);

  public ACG.Polyline Convert(SOG.Circle target)
  {
    if (target.radius == null)
    {
      throw new SpeckleConversionException("Conversion failed: Circle doesn't have a radius");
    }
    if (
      target.plane.normal.x != 0 || target.plane.normal.y != 0 || target.plane.xdir.z != 0 || target.plane.ydir.z != 0
    )
    {
      throw new ArgumentException("Only Circles in XY plane are supported");
    }

    // create a native ArcGIS circle segment
    ACG.MapPoint centerPt = _pointConverter.Convert(target.plane.origin);

    double scaleFactor = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    ACG.EllipticArcSegment circleSegment = ACG.EllipticArcBuilderEx.CreateCircle(
      new ACG.Coordinate2D(centerPt.X, centerPt.Y),
      (double)target.radius * scaleFactor,
      ACG.ArcOrientation.ArcClockwise,
      _contextStack.Current.Document.Map.SpatialReference
    );

    return new ACG.PolylineBuilderEx(
      circleSegment,
      ACG.AttributeFlags.HasZ,
      _contextStack.Current.Document.Map.SpatialReference
    ).ToGeometry();
  }
}

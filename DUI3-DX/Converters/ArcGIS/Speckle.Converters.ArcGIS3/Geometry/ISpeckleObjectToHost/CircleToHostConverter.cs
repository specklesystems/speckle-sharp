using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Circle, ACG.Polyline>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;

  public CircleToHostConverter(IRawConversion<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Circle)target);

  public ACG.Polyline RawConvert(SOG.Circle target)
  {
    if (target.radius == null)
    {
      throw new SpeckleConversionException("Conversion failed: Circle doesn't have a radius");
    }

    // create a native ArcGIS circle segment, turn into a native Polyline
    ACG.MapPoint centerPt = _pointConverter.RawConvert(target.plane.origin);
    ACG.EllipticArcSegment circleSegment = ACG.EllipticArcBuilderEx.CreateCircle(
      new ACG.Coordinate2D(centerPt.X, centerPt.Y),
      (double)target.radius,
      ACG.ArcOrientation.ArcClockwise
    );

    var circlePolyline = new ACG.PolylineBuilderEx(circleSegment, ACG.AttributeFlags.HasZ).ToGeometry();

    return circlePolyline;
  }
}

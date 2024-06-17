using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Arc, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;

  public CurveToHostConverter(ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => Convert((SOG.Arc)target);

  public ACG.Polyline Convert(SOG.Arc target)
  {
    // MapPoint fromPt = MapPointBuilderEx.CreateMapPoint(target.startPoint, 1);
    ACG.MapPoint fromPt = _pointConverter.Convert(target.startPoint);
    ACG.MapPoint toPt = _pointConverter.Convert(target.endPoint);
    ACG.MapPoint midPt = _pointConverter.Convert(target.midPoint);
    ACG.Coordinate2D interiorPt = new(midPt);

    ACG.EllipticArcSegment segment = ACG.EllipticArcBuilderEx.CreateCircularArc(fromPt, toPt, interiorPt);

    return new ACG.PolylineBuilderEx(segment, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

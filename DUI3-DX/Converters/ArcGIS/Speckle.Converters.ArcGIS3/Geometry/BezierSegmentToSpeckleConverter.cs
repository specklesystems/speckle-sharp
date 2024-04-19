using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(ACG.CubicBezierSegment), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BezierSegmentToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<ACG.CubicBezierSegment, SOG.Polyline>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly IRawConversion<ArcMapPoint, SOG.Point> _pointConverter;

  public BezierSegmentToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    IRawConversion<ArcMapPoint, SOG.Point> pointConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((ACG.CubicBezierSegment)target);

  public SOG.Polyline RawConvert(ACG.CubicBezierSegment target)
  {
    // Determine the number of vertices to create along the arc
    int numVertices = Math.Max((int)target.Length, 3); // Determine based on desired segment length or other criteria
    List<SOG.Point> points = new();

    // Calculate the vertices along the curve
    for (int i = 0; i <= numVertices; i++)
    {
      double t = i / (double)numVertices;

      // Calculate the point using the cubic Bezier formula
      double x =
        (1 - t) * (1 - t) * (1 - t) * target.StartPoint.X
        + 3 * (1 - t) * (1 - t) * t * target.ControlPoint1.X
        + 3 * (1 - t) * t * t * target.ControlPoint2.X
        + t * t * t * target.EndPoint.X;

      double y =
        (1 - t) * (1 - t) * (1 - t) * target.StartPoint.Y
        + 3 * (1 - t) * (1 - t) * t * target.ControlPoint1.Y
        + 3 * (1 - t) * t * t * target.ControlPoint2.Y
        + t * t * t * target.EndPoint.Y;

      ArcMapPoint pointOnCurve = ACG.MapPointBuilderEx.CreateMapPoint(x, y, target.SpatialReference);
      points.Add(_pointConverter.RawConvert(pointOnCurve));
    }

    // create Speckle Polyline
    SOG.Polyline polyline =
      new(points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(), _contextStack.Current.SpeckleUnits)
      {
        // bbox = box,
        length = target.Length
      };
    return polyline;
  }
}

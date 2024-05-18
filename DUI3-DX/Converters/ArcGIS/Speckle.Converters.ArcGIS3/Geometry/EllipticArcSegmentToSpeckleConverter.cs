using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class EllipticArcToSpeckleConverter : IRawConversion<ACG.EllipticArcSegment, List<SOG.Point>>
{
  private readonly IRawConversion<ACG.MapPoint, SOG.Point> _pointConverter;

  public EllipticArcToSpeckleConverter(IRawConversion<ACG.MapPoint, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public List<SOG.Point> RawConvert(ACG.EllipticArcSegment target)
  {
    // Determine the number of vertices to create along the arc
    int numVertices = Math.Max((int)target.Length, 3); // Determine based on desired segment length or other criteria
    List<SOG.Point> points = new();

    // get correct direction
    int coeff = 1;
    double fullAngle = target.EndAngle - target.StartAngle;
    double angleStart = target.StartAngle;

    // define the direction
    if (
      !((target.IsCounterClockwise is false || fullAngle >= 0) && (target.IsCounterClockwise is true || fullAngle < 0))
    )
    {
      fullAngle = Math.PI * 2 - Math.Abs(fullAngle);
      if (target.IsCounterClockwise is false)
      {
        coeff = -1;
      }
    }

    // Calculate the vertices along the arc
    for (int i = 0; i <= numVertices; i++)
    {
      // Calculate the point along the arc
      double angle = angleStart + coeff * fullAngle * (i / (double)numVertices);
      ACG.MapPoint pointOnArc = ACG.MapPointBuilderEx.CreateMapPoint(
        target.CenterPoint.X + target.SemiMajorAxis * Math.Cos(angle),
        target.CenterPoint.Y + target.SemiMinorAxis * Math.Sin(angle),
        target.SpatialReference
      );

      points.Add(_pointConverter.RawConvert(pointOnArc));
    }

    return points;
  }
}

using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Circle, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;

  public CircleToHostConverter(ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Circle)target);

  public ACG.Polyline RawConvert(SOG.Circle target)
  {
    // Determine the number of vertices to create along the cirlce
    int numVertices = Math.Max((int)target.length, 100); // Determine based on desired segment length or other criteria
    List<SOG.Point> pointsOriginal = new();

    if (target.radius == null)
    {
      throw new SpeckleConversionException("Conversion failed: Circle doesn't have a radius");
    }

    // Calculate the vertices along the arc
    for (int i = 0; i <= numVertices; i++)
    {
      // Calculate the point along the arc
      double angle = 2 * Math.PI * (i / (double)numVertices);
      SOG.Point pointOnCircle =
        new(
          target.plane.origin.x + (double)target.radius * Math.Cos(angle),
          target.plane.origin.y + (double)target.radius * Math.Sin(angle),
          target.plane.origin.z
        );

      pointsOriginal.Add(pointOnCircle);
    }
    if (pointsOriginal[0] != pointsOriginal[^1])
    {
      pointsOriginal.Add(pointsOriginal[0]);
    }

    var points = pointsOriginal.Select(x => _pointConverter.RawConvert(x));
    return new ACG.PolylineBuilderEx(points, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

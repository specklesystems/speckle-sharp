using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

//TODO: Ellipses don't convert correctly, see Autocad test stream
// [NameAndRankValue(nameof(SOG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class EllipseToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Ellipse, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;

  public EllipseToHostConverter(ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => Convert((SOG.Ellipse)target);

  public ACG.Polyline Convert(SOG.Ellipse target)
  {
    // Determine the number of vertices to create along the Ellipse
    int numVertices = Math.Max((int)target.length, 100); // Determine based on desired segment length or other criteria
    List<SOG.Point> pointsOriginal = new();

    if (target.firstRadius == null || target.secondRadius == null)
    {
      throw new SpeckleConversionException("Conversion failed: Ellipse doesn't have 1st and 2nd radius");
    }

    // Calculate the vertices along the arc
    for (int i = 0; i <= numVertices; i++)
    {
      // Calculate the point along the arc
      double angle = 2 * Math.PI * (i / (double)numVertices);
      SOG.Point pointOnEllipse =
        new(
          target.plane.origin.x + (double)target.secondRadius * Math.Cos(angle),
          target.plane.origin.y + (double)target.firstRadius * Math.Sin(angle),
          target.plane.origin.z
        );

      pointsOriginal.Add(pointOnEllipse);
    }
    if (pointsOriginal[0] != pointsOriginal[^1])
    {
      pointsOriginal.Add(pointsOriginal[0]);
    }

    var points = pointsOriginal.Select(x => _pointConverter.Convert(x));
    return new ACG.PolylineBuilderEx(points, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

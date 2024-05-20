using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

//TODO: Ellipses don't convert correctly, see Autocad test stream
//[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Arc, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;

  public CurveToHostConverter(ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Arc)target);

  public ACG.Polyline RawConvert(SOG.Arc target)
  {
    // Determine the number of vertices to create along the arc
    int numVertices = Math.Max((int)target.length, 50); // Determine based on desired segment length or other criteria
    List<SOG.Point> pointsOriginal = new();

    // get correct direction
    double? angleStart = target.startAngle;
    double? fullAngle = target.endAngle - target.startAngle;
    double? radius = target.radius;

    if (angleStart == null || fullAngle == null || radius == null)
    {
      throw new SpeckleConversionException("Conversion failed: Arc doesn't have start & end angle or radius");
    }

    // Calculate the vertices along the arc
    for (int i = 0; i <= numVertices; i++)
    {
      // Calculate the point along the arc
      double angle = (double)angleStart + (double)fullAngle * (i / (double)numVertices);
      SOG.Point pointOnArc =
        new(
          target.plane.origin.x + (double)radius * Math.Cos(angle),
          target.plane.origin.y + (double)radius * Math.Sin(angle),
          target.plane.origin.z
        );

      pointsOriginal.Add(pointOnArc);
    }

    var points = pointsOriginal.Select(x => _pointConverter.RawConvert(x));
    return new ACG.PolylineBuilderEx(points, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class SegmentCollectionToSpeckleConverter : ITypedConverter<ACG.ReadOnlySegmentCollection, SOG.Polyline>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;
  private readonly ITypedConverter<ACG.MapPoint, SOG.Point> _pointConverter;

  public SegmentCollectionToSpeckleConverter(
    IConversionContextStack<Map, ACG.Unit> contextStack,
    ITypedConverter<ACG.MapPoint, SOG.Point> pointConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
  }

  public SOG.Polyline Convert(ACG.ReadOnlySegmentCollection target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    double len = 0;

    List<SOG.Point> points = new();
    foreach (var segment in target)
    {
      len += segment.Length;

      // specific conversion per segment type
      switch (segment.SegmentType)
      {
        case ACG.SegmentType.Line:
          points = AddPtsToPolylinePts(
            points,
            new List<SOG.Point>()
            {
              _pointConverter.RawConvert(segment.StartPoint),
              _pointConverter.RawConvert(segment.EndPoint)
            }
          );
        default:
          throw new SpeckleConversionException($"Segment of type '{segment.SegmentType}' cannot be converted");
      }
    }
    SOG.Polyline polyline =
      new(points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(), _contextStack.Current.SpeckleUnits) { };

    return polyline;
  }

  private List<SOG.Point> AddPtsToPolylinePts(List<SOG.Point> points, List<SOG.Point> newSegmentPts)
  {
    if (points.Count == 0 || points[^1] != newSegmentPts[0])
    {
      points.AddRange(newSegmentPts);
    }
    else
    {
      points.AddRange(newSegmentPts.GetRange(1, newSegmentPts.Count - 1));
    }
    return points;
  }
}

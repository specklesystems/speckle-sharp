using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.ToSpeckle.Raw;

public class SegmentCollectionToSpeckleConverter : ITypedConverter<ACG.ReadOnlySegmentCollection, SOG.Polyline>
{
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;
  private readonly ITypedConverter<ACG.MapPoint, SOG.Point> _pointConverter;

  public SegmentCollectionToSpeckleConverter(
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack,
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

      if (segment.SegmentType != ACG.SegmentType.Line)
      {
        // densify the segments with curves using precision value of the Map's Spatial Reference
        ACG.Polyline polylineFromSegment = new ACG.PolylineBuilderEx(
          segment,
          ACG.AttributeFlags.HasZ,
          _contextStack.Current.Document.Map.SpatialReference
        ).ToGeometry();

        double tolerance = _contextStack.Current.Document.Map.SpatialReference.XYTolerance;
        double conversionFactorToMeter = _contextStack.Current.Document.Map.SpatialReference.Unit.ConversionFactor;
        var densifiedPolyline = ACG.GeometryEngine.Instance.DensifyByDeviation(
          polylineFromSegment,
          tolerance * conversionFactorToMeter
        );
        if (densifiedPolyline == null)
        {
          throw new ArgumentException("Segment densification failed");
        }

        ACG.Polyline polylineToConvert = (ACG.Polyline)densifiedPolyline;
        // add points from each segment of the densified original segment
        ACG.ReadOnlyPartCollection subParts = polylineToConvert.Parts;
        foreach (ACG.ReadOnlySegmentCollection subSegments in subParts)
        {
          foreach (ACG.Segment? subSegment in subSegments)
          {
            points = AddPtsToPolylinePts(
              points,
              new List<SOG.Point>()
              {
                _pointConverter.Convert(subSegment.StartPoint),
                _pointConverter.Convert(subSegment.EndPoint)
              }
            );
          }
        }
      }
      else
      {
        points = AddPtsToPolylinePts(
          points,
          new List<SOG.Point>()
          {
            _pointConverter.Convert(segment.StartPoint),
            _pointConverter.Convert(segment.EndPoint)
          }
        );
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

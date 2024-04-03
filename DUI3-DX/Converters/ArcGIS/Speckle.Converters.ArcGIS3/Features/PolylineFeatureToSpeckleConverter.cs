using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolyineFeatureToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Polyline, GisFeature>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<ArcMapPoint, SOG.Point> _pointConverter;
  private readonly IRawConversion<EllipticArcSegment, SOG.Polyline> _arcConverter;
  private readonly IRawConversion<CubicBezierSegment, SOG.Polyline> _bezierConverter;

  public PolyineFeatureToSpeckleConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<ArcMapPoint, SOG.Point> pointConverter,
    IRawConversion<EllipticArcSegment, SOG.Polyline> arcConverter,
    IRawConversion<CubicBezierSegment, SOG.Polyline> bezierConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
    _arcConverter = arcConverter;
    _bezierConverter = bezierConverter;
  }

  public Base Convert(object target) => RawConvert((Polyline)target);

  public GisFeature RawConvert(Polyline target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    List<Base> polylineList = new();
    double len = 0;

    // or use foreach pattern
    foreach (var part in target.Parts)
    {
      List<SOG.Point> points = new();
      foreach (var segment in part)
      {
        len += segment.Length;

        // perhaps do something specific per segment type
        switch (segment.SegmentType)
        {
          case SegmentType.Line:
            points.Add(_pointConverter.RawConvert(segment.StartPoint));
            points.Add(_pointConverter.RawConvert(segment.EndPoint));
            break;
          case SegmentType.Bezier:
            var segmentBezier = (CubicBezierSegment)segment;
            points.AddRange(_bezierConverter.RawConvert(segmentBezier).GetPoints());
            break;
          case SegmentType.EllipticArc:
            var segmentElliptic = (EllipticArcSegment)segment;
            points.AddRange(_arcConverter.RawConvert(segmentElliptic).GetPoints());
            break;
        }
      }
      // var box = _boxConverter.RawConvert(target.Extent);
      SOG.Polyline polylinePart =
        new(points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(), _contextStack.Current.SpeckleUnits)
        {
          // bbox = box,
          length = target.Length
        };
      // return polylinePart;
      polylineList.Add(polylinePart);
    }
    return new GisFeature { geometry = polylineList };

    //return polylineList;
  }
}

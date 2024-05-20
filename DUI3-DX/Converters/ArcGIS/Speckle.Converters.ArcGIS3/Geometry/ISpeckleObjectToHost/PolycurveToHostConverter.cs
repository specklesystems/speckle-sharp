using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolycurveToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Polycurve, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IRootToHostConverter _converter;

  public PolycurveToHostConverter(
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    IRootToHostConverter converter
  )
  {
    _pointConverter = pointConverter;
    _converter = converter;
  }

  public object Convert(Base target) => Convert((SOG.Polycurve)target);

  public ACG.Polyline Convert(SOG.Polycurve target)
  {
    List<ACG.MapPoint> points = new();
    foreach (var segment in target.segments)
    {
      if (segment is SOG.Arc)
      {
        throw new NotImplementedException("Polycurves with arc segments are not supported");
      }
      ACG.Polyline converted = (ACG.Polyline)_converter.Convert((Base)segment);
      List<ACG.MapPoint> newPts = converted.Points.ToList();

      // reverse new segment if needed
      if (points.Count > 0 && newPts.Count > 0 && points[^1] != newPts[0] && points[^1] == newPts[^1])
      {
        newPts.Reverse();
      }
      points.AddRange(newPts);
    }

    return new ACG.PolylineBuilderEx(points, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

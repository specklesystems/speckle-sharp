using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolycurveToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Polycurve, ACG.Polyline>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly ISpeckleConverterToHost _toHostConverter;

  public PolycurveToHostConverter(
    IRawConversion<SOG.Point, ACG.MapPoint> pointConverter,
    ISpeckleConverterToHost toHostConverter
  )
  {
    _pointConverter = pointConverter;
    _toHostConverter = toHostConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Polycurve)target);

  public ACG.Polyline RawConvert(SOG.Polycurve target)
  {
    List<ACG.MapPoint> points = new();
    foreach (var segment in target.segments)
    {
      ACG.Polyline converted = (ACG.Polyline)_toHostConverter.Convert((Base)segment);
      points.AddRange(converted.Points);
    }

    return new ACG.PolylineBuilderEx(points, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

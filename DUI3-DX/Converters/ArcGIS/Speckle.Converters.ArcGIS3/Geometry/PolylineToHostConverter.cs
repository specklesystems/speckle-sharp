using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PolylineToHostConverter : IRawConversion<List<SOG.Polyline>, ACG.Polyline>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;

  public PolylineToHostConverter(IRawConversion<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public ACG.Polyline RawConvert(List<SOG.Polyline> target)
  {
    // only 1 geometry expected
    foreach (SOG.Polyline poly in target)
    {
      var points = poly.GetPoints().Select(x => _pointConverter.RawConvert(x));
      return new ACG.PolylineBuilderEx(points, ACG.AttributeFlags.HasZ).ToGeometry();
    }
    throw new SpeckleConversionException("Conversion was not successful");
  }
}

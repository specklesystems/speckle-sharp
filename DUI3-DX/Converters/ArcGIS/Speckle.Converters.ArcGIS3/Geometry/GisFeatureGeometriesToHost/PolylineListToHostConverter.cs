using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry.GisFeatureGeometriesToHost;

public class PolylineListToHostConverter : IRawConversion<List<SOG.Polyline>, ACG.Polyline>
{
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public PolylineListToHostConverter(IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public ACG.Polyline RawConvert(List<SOG.Polyline> target)
  {
    if (target.Count == 0)
    {
      throw new SpeckleConversionException("Feature contains no geometries");
    }
    List<ACG.Polyline> polyList = new();
    foreach (SOG.Polyline poly in target)
    {
      ACG.Polyline arcgisPoly = _polylineConverter.RawConvert(poly);
      polyList.Add(arcgisPoly);
    }
    return new ACG.PolylineBuilderEx(polyList, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

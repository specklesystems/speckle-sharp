using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry.GisFeatureGeometriesToHost;

public class PolylineListToHostConverter : ITypedConverter<List<SOG.Polyline>, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public PolylineListToHostConverter(ITypedConverter<SOG.Polyline, ACG.Polyline> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public ACG.Polyline Convert(List<SOG.Polyline> target)
  {
    if (target.Count == 0)
    {
      throw new SpeckleConversionException("Feature contains no geometries");
    }
    List<ACG.Polyline> polyList = new();
    foreach (SOG.Polyline poly in target)
    {
      ACG.Polyline arcgisPoly = _polylineConverter.Convert(poly);
      polyList.Add(arcgisPoly);
    }
    return new ACG.PolylineBuilderEx(polyList, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class MultipatchToHostConverter : IRawConversion<List<SGIS.GisMultipatchGeometry>, ACG.Multipatch>
{
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public MultipatchToHostConverter(IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public ACG.Multipatch RawConvert(List<SGIS.GisMultipatchGeometry> target)
  {
    throw new SpeckleConversionException("Something went wrong");
  }
}

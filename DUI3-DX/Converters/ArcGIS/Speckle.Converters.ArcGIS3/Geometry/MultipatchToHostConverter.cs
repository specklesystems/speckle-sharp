using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class MultipatchToHostConverter : IRawConversion<SOG.Mesh, ACG.Multipatch>
{
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public MultipatchToHostConverter(IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public ACG.Multipatch RawConvert(SOG.Mesh target)
  {
    throw new SpeckleConversionException("Something went wrong");
  }
}

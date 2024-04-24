using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PolylineToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Polyline, ACG.Polyline>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;

  public PolylineToHostConverter(IRawConversion<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Polyline)target);

  public ACG.Polyline RawConvert(SOG.Polyline target)
  {
    var points = target.GetPoints().Select(x => _pointConverter.RawConvert(x));
    PolylineBuilder polyBuilder = new(points);
    ACG.Polyline polyline = polyBuilder.ToGeometry();
    polyBuilder.Dispose();

    return polyline;
  }
}

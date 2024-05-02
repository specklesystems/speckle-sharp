using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
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
    return new ACG.PolylineBuilderEx(points, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}

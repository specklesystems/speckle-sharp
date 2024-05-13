using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToHostConverter : ISpeckleObjectToHostConversion
{
  private readonly IRawConversion<List<SOG.Point>, ACG.Multipoint> _pointConverter;

  public PointToHostConverter(IRawConversion<List<SOG.Point>, ACG.Multipoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => _pointConverter.RawConvert(new List<SOG.Point> { (SOG.Point)target });
}

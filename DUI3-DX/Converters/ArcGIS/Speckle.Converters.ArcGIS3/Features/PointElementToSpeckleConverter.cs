using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.GIS;
using ArcGIS.Core.Data;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(Row), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointElementToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Row, PointElement>
{
  private readonly IRawConversion<MapPoint, Point> _pointConverter;

  public PointElementToSpeckleConverter(IRawConversion<MapPoint, Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public PointElement RawConvert(Row target)
  {
    var geometry = new List<Point>();
    MapPoint shape = (MapPoint)target["SHAPE"];
    var pt = _pointConverter.RawConvert(shape);
    geometry.Add(pt);

    // get attributes
    var attributes = new Base();
    // IReadOnlyList<Field> fields = target.GetFields();

    return new PointElement { geometry = geometry, attributes = attributes };
  }
}

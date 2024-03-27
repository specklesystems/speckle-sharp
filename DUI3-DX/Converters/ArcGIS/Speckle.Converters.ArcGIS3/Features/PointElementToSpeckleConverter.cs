using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.GIS;
using ArcGIS.Core.Data;
using Objects.BuiltElements.TeklaStructures;

namespace Speckle.Converters.ArcGIS3.Features;

public class PointElementToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Row, PointElement>
{
  private readonly IRawConversion<MapPoint, Point> _pointConverter;

  public PointElementToSpeckleConverter(IRawConversion<MapPoint, Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public PointElement RawConvert(Row feature)
  {
    var geometry = new List<Point>();
    MapPoint shape = (MapPoint)feature["SHAPE"];
    var pt = _pointConverter.RawConvert(shape);
    geometry.Add(pt);

    // get attributes
    var attributes = new Base();
    IReadOnlyList<Field> fields = feature.GetFields();

    return new PointElement(geometry, attributes);
  }
}

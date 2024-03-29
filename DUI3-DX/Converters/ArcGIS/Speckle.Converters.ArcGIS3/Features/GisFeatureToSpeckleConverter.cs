using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.GIS;
using ArcGIS.Core.Data;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(Row), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class GisFeatureToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Row, GisFeature>
{
  private readonly IRawConversion<MapPoint, Point> _pointConverter;

  public GisFeatureToSpeckleConverter(IRawConversion<MapPoint, Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    var geometry = new List<Base>();
    MapPoint shape = (MapPoint)target["Shape"];
    var nativeGeometryType = shape.GetType().ToString();
    var pt = _pointConverter.RawConvert(shape);
    geometry.Add(pt);

    // get attributes
    var attributes = new Base();
    IReadOnlyList<Field> fields = target.GetFields();
    int i = 0;
    foreach (Field field in fields)
    {
      string name = field.Name;
      if (name != "Shape")
      {
        var value = target.GetOriginalValue(i); // can be null
        attributes[name] = value;
      }
      i++;
    }

    return new GisFeature
    {
      geometry = geometry,
      attributes = attributes,
      nativeGeometryType = nativeGeometryType,
    };
  }
}

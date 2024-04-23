using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisFeatureToSpeckleConverter : IRawConversion<Row, GisFeature>
{
  private readonly IRawConversion<ACG.Geometry, IReadOnlyList<Base>> _geometryConverter;

  public GisFeatureToSpeckleConverter(IRawConversion<ACG.Geometry, IReadOnlyList<Base>> geometryConverter)
  {
    _geometryConverter = geometryConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    var shape = (ACG.Geometry)target["Shape"];
    var speckleShapes = _geometryConverter.RawConvert(shape).ToList();

    // get attributes
    var attributes = new Base();
    IReadOnlyList<Field> fields = target.GetFields();
    int i = 0;
    foreach (Field field in fields)
    {
      string name = field.Name;

      // breaks on Raster Field type
      if (name != "Shape" && field.FieldType.ToString() != "Raster")
      {
        var value = target.GetOriginalValue(i); // can be null
        attributes[name] = value;
      }
      i++;
    }
    return new GisFeature(speckleShapes, attributes);
  }
}

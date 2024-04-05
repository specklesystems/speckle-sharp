using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(Row), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class GisFeatureToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Row, GisFeature>
{
  private readonly IRawConversion<ArcGIS.Core.Geometry.Geometry, Base> _geometryConverter;

  public GisFeatureToSpeckleConverter(IRawConversion<ArcGIS.Core.Geometry.Geometry, Base> geometryConverter)
  {
    _geometryConverter = geometryConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    var shape = (ArcGIS.Core.Geometry.Geometry)target["Shape"];
    var speckleShapes = new List<Base>() { _geometryConverter.RawConvert(shape) };

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

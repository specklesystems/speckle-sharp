using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.GIS;
using ArcGIS.Core.Data;
using Speckle.Converters.Common;
using Speckle.Autofac.DependencyInjection;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(Row), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class GisFeatureToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Row, GisFeature>
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;
  private readonly IRawConversion<MapPoint, Point> _pointConverter;
  private readonly IRawConversion<ArcGIS.Core.Geometry.Geometry, List<Base>> _geometryConverter;

  public GisFeatureToSpeckleConverter(
    IRawConversion<MapPoint, Point> pointConverter,
    IFactory<string, IHostObjectToSpeckleConversion> toSpeckle,
    IRawConversion<ArcGIS.Core.Geometry.Geometry, List<Base>> geometryConverter
  )
  {
    _toSpeckle = toSpeckle;
    _pointConverter = pointConverter;
    _geometryConverter = geometryConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    // GisFeature newFeature;
    var shape = (ArcGIS.Core.Geometry.Geometry)target["Shape"];
    var speckleShapes = _geometryConverter.RawConvert(shape);

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

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

  public GisFeatureToSpeckleConverter(
    IRawConversion<MapPoint, Point> pointConverter,
    IFactory<string, IHostObjectToSpeckleConversion> toSpeckle
  )
  {
    _toSpeckle = toSpeckle;
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    GisFeature newFeature;
    var shape = target["Shape"];
    Type type = shape.GetType();

    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(type.Name);

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {type.Name}");
      }
      newFeature = (GisFeature)objectConverter.Convert(shape);
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }

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
    newFeature.attributes = attributes;
    return newFeature;
  }
}

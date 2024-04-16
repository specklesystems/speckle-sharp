using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(FeatureLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<FeatureLayer, VectorLayer>
{
  private readonly IRawConversion<Row, GisFeature> _gisFeatureConverter;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public VectorLayerToSpeckleConverter(
    IRawConversion<Row, GisFeature> gisFeatureConverter,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _gisFeatureConverter = gisFeatureConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target)
  {
    return RawConvert((FeatureLayer)target);
  }

  public VectorLayer RawConvert(FeatureLayer target)
  {
    var speckleLayer = new VectorLayer();
    var spatialRef = target.GetSpatialReference();

    // get active map CRS if layer CRS is empty
    if (spatialRef.Unit is null)
    {
      spatialRef = _contextStack.Current.Document.SpatialReference;
    }
    speckleLayer.crs = new CRS
    {
      wkt = spatialRef.Wkt,
      name = spatialRef.Name,
      units_native = spatialRef.Unit.ToString(),
    };
    speckleLayer.name = target.Name;

    // get feature class fields
    var attributes = new Base();
    IReadOnlyList<Field> fields = target.GetTable().GetDefinition().GetFields();
    foreach (Field field in fields)
    {
      string name = field.Name;
      // breaks on Raster Field type when assigning indiv. GisFeature values
      if (name != "Shape" && field.FieldType.ToString() != "Raster")
      {
        attributes[name] = field.FieldType;
      }
    }
    speckleLayer.attributes = attributes;
    speckleLayer.nativeGeomType = target.ShapeType.ToString();

    // search the rows

    // RowCursor is IDisposable but is not being correctly picked up by IDE warnings.
    // This means we need to be carefully adding using statements based on the API documentation coming from each method/class

    using (RowCursor rowCursor = target.Search())
    {
      while (rowCursor.MoveNext())
      {
        // Same IDisposable issue appears to happen on Row class too. Docs say it should always be disposed of manually by the caller.
        using (Row row = rowCursor.Current)
        {
          var element = _gisFeatureConverter.RawConvert(row);
          speckleLayer.elements.Add(element);
        }
      }

      return speckleLayer;
    }
  }
}

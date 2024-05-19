using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using Speckle.Converters.ArcGIS3.Utils;
using ArcGIS.Core.CIM;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(FeatureLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<FeatureLayer, VectorLayer>
{
  private readonly IRawConversion<Row, GisFeature> _gisFeatureConverter;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISFieldUtils _fieldsUtils;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public VectorLayerToSpeckleConverter(
    IRawConversion<Row, GisFeature> gisFeatureConverter,
    IFeatureClassUtils featureClassUtils,
    IArcGISFieldUtils fieldsUtils,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _gisFeatureConverter = gisFeatureConverter;
    _featureClassUtils = featureClassUtils;
    _fieldsUtils = fieldsUtils;
    _contextStack = contextStack;
  }

  public Base Convert(object target)
  {
    return RawConvert((FeatureLayer)target);
  }

  private string AssignSpeckleGeometryType(esriGeometryType nativeGeometryType)
  {
    return nativeGeometryType switch
    {
      esriGeometryType.esriGeometryMultipoint => GISLayerGeometryType.POINT,
      esriGeometryType.esriGeometryPoint => GISLayerGeometryType.POINT,
      esriGeometryType.esriGeometryLine => GISLayerGeometryType.POLYLINE,
      esriGeometryType.esriGeometryPolyline => GISLayerGeometryType.POLYLINE,
      esriGeometryType.esriGeometryPolygon => GISLayerGeometryType.POLYGON,
      esriGeometryType.esriGeometryMultiPatch => GISLayerGeometryType.MULTIPATCH,
      _ => GISLayerGeometryType.NONE,
    };
  }

  public VectorLayer RawConvert(FeatureLayer target)
  {
    VectorLayer speckleLayer = new();

    // get document CRS (for writing geometry coords)
    var spatialRef = _contextStack.Current.Document.SpatialReference;
    speckleLayer.crs = new CRS
    {
      wkt = spatialRef.Wkt,
      name = spatialRef.Name,
      units_native = spatialRef.Unit.ToString(),
    };

    // other properties
    speckleLayer.name = target.Name;
    speckleLayer.units = _contextStack.Current.SpeckleUnits;

    // get feature class fields
    var allLayerAttributes = new Base();
    var dispayTable = target as IDisplayTable;
    IReadOnlyList<FieldDescription> allFieldDescriptions = dispayTable.GetFieldDescriptions();
    List<FieldDescription> addedFieldDescriptions = new();
    foreach (FieldDescription field in allFieldDescriptions)
    {
      if (field.IsVisible)
      {
        string name = field.Name;
        if (
          field.Type == FieldType.Geometry
          || field.Type == FieldType.Raster
          || field.Type == FieldType.XML
          || field.Type == FieldType.Blob
        )
        {
          continue;
        }
        addedFieldDescriptions.Add(field);
        allLayerAttributes[name] = (int)field.Type;
      }
    }
    speckleLayer.attributes = allLayerAttributes;

    // get a simple geometry type
    string spekleGeometryType = AssignSpeckleGeometryType(target.ShapeType);
    speckleLayer.geomType = spekleGeometryType;

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
          GisFeature element = _gisFeatureConverter.RawConvert(row);

          // replace element "attributes", to remove those non-visible on Layer level
          Base elementAttributes = new();
          foreach (FieldDescription field in addedFieldDescriptions)
          {
            if (field.IsVisible)
            {
              elementAttributes[field.Name] = element.attributes[field.Name];
            }
          }
          element.attributes = elementAttributes;
          speckleLayer.elements.Add(element);
        }
      }
    }

    return speckleLayer;
  }
}

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

      if (name == "Shape")
      {
        continue;
      }
      // TODO more field filters (e.g. visible only)
      attributes[name] = field.FieldType;
    }
    speckleLayer.attributes = attributes;
    speckleLayer.nativeGeomType = target.ShapeType.ToString();

    // get a simple geometry type 
    string spekleGeometryType = string.Empty;
    if (speckleLayer.nativeGeomType.ToLower().Contains("point"))
    {
      spekleGeometryType = "Point";
    }
    else if (speckleLayer.nativeGeomType.ToLower().Contains("polyline"))
    {
      spekleGeometryType = "Polyline";
    }
    else if (speckleLayer.nativeGeomType.ToLower().Contains("polygon"))
    {
      spekleGeometryType = "Polygon";
    }
    else if (speckleLayer.nativeGeomType.ToLower().Contains("multipatch"))
    {
      spekleGeometryType = "Multipatch";
    }


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
          speckleLayer.elements.Add(element);
          if (spekleGeometryType == "Multipatch" && element.geometry != null && element.displayValue != null && element.geometry.Count > 0 && element.displayValue.Count == 0)
          {
            spekleGeometryType = "Polygon";
          }
        }
      }
    }

    // differentiate between Ring Multipatches and Mesh Multipatches
    if (speckleLayer.nativeGeomType.Contains("Multipatch") && )

    return speckleLayer;
  }
}

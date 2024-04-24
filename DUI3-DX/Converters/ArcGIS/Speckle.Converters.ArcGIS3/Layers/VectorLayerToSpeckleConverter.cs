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

  private string SpeckleGeometryType(string nativeGeometryType)
  {
    string spekleGeometryType = "None";
    if (nativeGeometryType.ToLower().Contains("point"))
    {
      spekleGeometryType = "Point";
    }
    else if (nativeGeometryType.ToLower().Contains("polyline"))
    {
      spekleGeometryType = "Polyline";
    }
    else if (nativeGeometryType.ToLower().Contains("polygon"))
    {
      spekleGeometryType = "Polygon";
    }
    else if (nativeGeometryType.ToLower().Contains("multipatch"))
    {
      spekleGeometryType = "Multipatch";
    }
    return spekleGeometryType;
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
    var layerAttributes = new Base();
    var dispayTable = target as IDisplayTable;
    IReadOnlyList<FieldDescription> fieldDescriptions = dispayTable.GetFieldDescriptions();
    foreach (FieldDescription field in fieldDescriptions)
    {
      string name = field.Name;
      if (name == "Shape")
      {
        continue;
      }
      if (field.IsVisible)
      {
        layerAttributes[name] = field.Type;
      }
    }
    speckleLayer.attributes = layerAttributes;
    speckleLayer.nativeGeomType = target.ShapeType.ToString();

    // get a simple geometry type
    string spekleGeometryType = SpeckleGeometryType(speckleLayer.nativeGeomType);

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

          // replace "attributes", to remove non-visible layer attributes
          Base elementAttributes = new();
          foreach (FieldDescription field in fieldDescriptions)
          {
            elementAttributes[field.Name] = element.attributes[field.Name];
          }
          element.attributes = elementAttributes;
          speckleLayer.elements.Add(element);
        }
      }
    }

    speckleLayer.geomType = spekleGeometryType;

    return speckleLayer;
  }
}

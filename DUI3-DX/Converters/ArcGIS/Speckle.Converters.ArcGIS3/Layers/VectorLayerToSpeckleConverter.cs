using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(FeatureLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<FeatureLayer, VectorLayer>
{
  private readonly IRawConversion<Row, GisFeature> _gisFeatureConverter;

  public VectorLayerToSpeckleConverter(IRawConversion<Row, GisFeature> gisFeatureConverter)
  {
    _gisFeatureConverter = gisFeatureConverter;
  }

  public Base Convert(object target)
  {
    return RawConvert((FeatureLayer)target);
  }

  public VectorLayer RawConvert(FeatureLayer target)
  {
    var speckleLayer = new VectorLayer();
    var spatialRef = target.GetSpatialReference();
    speckleLayer.crs = new CRS
    {
      wkt = spatialRef.Wkt,
      name = spatialRef.Name,
      units_native = spatialRef.Unit.ToString(),
    };
    speckleLayer.name = target.Name;

    //Get the layer's definition
    // var lyrDefn = target.GetFeatureClass().GetDefinition();
    //Get the shape field of the feature class
    // string shapeField = lyrDefn.GetShapeField();
    //Index of the shape field
    // var shapeIndex = lyrDefn.FindField(shapeField);
    //Original geometry of the modified row
    // .GetOriginalValue(shapeIndex)


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

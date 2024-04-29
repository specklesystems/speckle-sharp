using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Layers;

public class FeatureClassToHostConverter : IRawConversion<VectorLayer, FeatureClass>
{
  private readonly IRawConversion<IReadOnlyList<Base>, ACG.Geometry> _gisGeometryConverter;
  private readonly IRawConversion<VectorLayer, LasDatasetLayer> _pointcloudLayerConverter;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;

  public FeatureClassToHostConverter(
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter,
    IRawConversion<VectorLayer, LasDatasetLayer> pointcloudLayerConverter,
    IFeatureClassUtils featureClassUtils,
    IArcGISProjectUtils arcGISProjectUtils
  )
  {
    _gisGeometryConverter = gisGeometryConverter;
    _pointcloudLayerConverter = pointcloudLayerConverter;
    _featureClassUtils = featureClassUtils;
    _arcGISProjectUtils = arcGISProjectUtils;
  }

  public object Convert(Base target) => RawConvert((VectorLayer)target);

  private const string FID_FIELD_NAME = "OBJECTID";

  public FeatureClass RawConvert(VectorLayer target)
  {
    GeometryType geomType = _featureClassUtils.GetLayerGeometryType(target);

    string databasePath = _arcGISProjectUtils.GetDatabasePath();
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(databasePath));
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // create Spatial Reference (i.e. Coordinate Reference System - CRS)
    string wktString = string.Empty;
    if (target.crs is not null && target.crs.wkt is not null)
    {
      wktString = target.crs.wkt.ToString();
    }
    SpatialReference spatialRef = SpatialReferenceBuilder.CreateSpatialReference(wktString);

    // create Fields
    List<FieldDescription> fields = new();
    List<string> fieldAdded = new();

    foreach (var field in target.attributes.GetMembers(DynamicBaseMemberType.Dynamic))
    {
      if (!fieldAdded.Contains(field.Key) && field.Key != FID_FIELD_NAME)
      {
        // POC: TODO check for the forbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588
        try
        {
          if (field.Value is not null)
          {
            FieldType fieldType = _featureClassUtils.GetFieldTypeFromInt((int)(long)field.Value);
            if (fieldType != FieldType.Raster)
            {
              fields.Add(new FieldDescription(field.Key, fieldType));
              fieldAdded.Add(field.Key);
            }
          }
          else
          {
            // log missing field
          }
        }
        catch (GeodatabaseFieldException)
        {
          // log missing field
        }
      }
    }
    // getting rid of forbidden symbols in the class name: adding a letter in the beginning
    // https://pro.arcgis.com/en/pro-app/3.1/tool-reference/tool-errors-and-warnings/001001-010000/tool-errors-and-warnings-00001-00025-000020.htm
    string featureClassName = "x" + target.id;

    // Create FeatureClass
    try
    {
      // POC: make sure class has a valid crs
      ShapeDescription shpDescription = new(geomType, spatialRef) { HasZ = true };
      FeatureClassDescription featureClassDescription = new(featureClassName, fields, shpDescription);
      FeatureClassToken featureClassToken = schemaBuilder.Create(featureClassDescription);
    }
    catch (ArgumentException ex)
    {
      // if name has invalid characters/combinations
      throw new ArgumentException($"{ex.Message}: {featureClassName}");
    }
    bool buildStatus = schemaBuilder.Build();
    if (!buildStatus)
    {
      // POC: log somewhere the error in building the feature class
      IReadOnlyList<string> errors = schemaBuilder.ErrorMessages;
    }

    try
    {
      FeatureClass newFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);
      // Add features to the FeatureClass
      List<GisFeature> gisFeatures = target.elements.Select(x => (GisFeature)x).ToList();
      geodatabase.ApplyEdits(() =>
      {
        _featureClassUtils.AddFeaturesToFeatureClass(newFeatureClass, gisFeatures, fieldAdded, _gisGeometryConverter);
      });

      return newFeatureClass;
    }
    catch (GeodatabaseException exObj)
    {
      // POC: review the exception
      throw new InvalidOperationException($"Something went wrong: {exObj.Message}");
    }
  }
}

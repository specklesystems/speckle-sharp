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
    List<FieldDescription> fields = _featureClassUtils.GetFieldsFromSpeckleLayer(target);

    // getting rid of forbidden symbols in the class name: adding a letter in the beginning
    // https://pro.arcgis.com/en/pro-app/3.1/tool-reference/tool-errors-and-warnings/001001-010000/tool-errors-and-warnings-00001-00025-000020.htm
    string featureClassName = "speckleID_" + target.id;

    // delete FeatureClass if already exists
    foreach (FeatureClassDefinition fClassDefinition in geodatabase.GetDefinitions<FeatureClassDefinition>())
    {
      // will cause GeodatabaseCatalogDatasetException if doesn't exist in the database
      if (fClassDefinition.GetName() == featureClassName)
      {
        FeatureClassDescription existingDescription = new(fClassDefinition);
        schemaBuilder.Delete(existingDescription);
        schemaBuilder.Build();
      }
    }

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
        _featureClassUtils.AddFeaturesToFeatureClass(newFeatureClass, gisFeatures, fields, _gisGeometryConverter);
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

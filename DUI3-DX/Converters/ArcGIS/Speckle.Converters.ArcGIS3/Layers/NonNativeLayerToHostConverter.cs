using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Layers;

public class NonNativeLayerToHostConverter : IRawConversion<List<ACG.Geometry>, List<string>>
{
  private readonly IRawConversion<IReadOnlyList<Base>, ACG.Geometry> _gisGeometryConverter;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public NonNativeLayerToHostConverter(
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter,
    IFeatureClassUtils featureClassUtils,
    IArcGISProjectUtils arcGISProjectUtils,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _gisGeometryConverter = gisGeometryConverter;
    _featureClassUtils = featureClassUtils;
    _arcGISProjectUtils = arcGISProjectUtils;
    _contextStack = contextStack;
  }

  public List<string> RawConvert(List<ACG.Geometry> target)
  {
    GeometryType geomType = _featureClassUtils.GetLayerGeometryType(target);

    string databasePath = _arcGISProjectUtils.GetDatabasePath();
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(databasePath));
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // get Spatial Reference from the document
    SpatialReference spatialRef = _contextStack.Current.Document.SpatialReference;

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
      throw new ArgumentException($"{ex.Message}: {featureClassName}", ex);
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

      return newFeatureClass.GetName();
      ;
    }
    catch (GeodatabaseException exObj)
    {
      // POC: review the exception
      throw exObj;
    }
  }
}

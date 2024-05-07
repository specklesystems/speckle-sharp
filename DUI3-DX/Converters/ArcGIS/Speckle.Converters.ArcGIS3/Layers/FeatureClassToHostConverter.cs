using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Layers;

public class FeatureClassToHostConverter : IRawConversion<VectorLayer, FeatureClass>
{
  private readonly IRawConversion<IReadOnlyList<Base>, ACG.Geometry> _gisGeometryConverter;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISFieldUtils _fieldsUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;

  public FeatureClassToHostConverter(
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter,
    IFeatureClassUtils featureClassUtils,
    IArcGISFieldUtils fieldsUtils,
    IArcGISProjectUtils arcGISProjectUtils
  )
  {
    _gisGeometryConverter = gisGeometryConverter;
    _featureClassUtils = featureClassUtils;
    _fieldsUtils = fieldsUtils;
    _arcGISProjectUtils = arcGISProjectUtils;
  }

  private List<GisFeature> RecoverOutdatedGisFeatures(VectorLayer target)
  {
    List<GisFeature> gisFeatures = new();
    foreach (Base baseElement in target.elements)
    {
      if (baseElement is GisFeature feature)
      {
        gisFeatures.Add(feature);
      }
      else
      {
        List<Base>? geometry = ((List<object>)baseElement["geometry"]).Select(x => (Base)x).ToList();
        Base attributes = (Base)baseElement["attributes"] == null ? new Base() : (Base)baseElement["attributes"];
        List<Base>? displayValue =
          baseElement["displayValue"] == null
            ? new List<Base>()
            : ((List<object>)baseElement["displayValue"]).Select(x => (Base)x).ToList();
        GisFeature newfeature =
          new()
          {
            geometry = geometry,
            attributes = attributes,
            displayValue = displayValue
          };
        gisFeatures.Add(newfeature);
      }
    }
    return gisFeatures;
  }

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
    List<FieldDescription> fields = _fieldsUtils.GetFieldsFromSpeckleLayer(target);

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
    catch (ArgumentException)
    {
      // POC: review the exception
      // if name has invalid characters/combinations
      throw;
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
      // backwards compatibility:
      List<GisFeature> gisFeatures = RecoverOutdatedGisFeatures(target);
      // Add features to the FeatureClass
      geodatabase.ApplyEdits(() =>
      {
        _featureClassUtils.AddFeaturesToFeatureClass(newFeatureClass, gisFeatures, fields, _gisGeometryConverter);
      });

      return newFeatureClass;
    }
    catch (GeodatabaseException)
    {
      // POC: review the exception
      throw;
    }
  }
}

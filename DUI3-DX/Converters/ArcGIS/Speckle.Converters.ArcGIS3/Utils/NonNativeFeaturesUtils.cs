using System.Diagnostics;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using Speckle.Core.Logging;

namespace Speckle.Converters.ArcGIS3.Utils;

public class NonNativeFeaturesUtils : INonNativeFeaturesUtils
{
  private readonly ITypedConverter<IReadOnlyList<Base>, ACG.Geometry> _gisGeometryConverter;
  private readonly IArcGISFieldUtils _fieldsUtils;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;

  public NonNativeFeaturesUtils(
    ITypedConverter<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter,
    IArcGISFieldUtils fieldsUtils,
    IFeatureClassUtils featureClassUtils,
    IArcGISProjectUtils arcGISProjectUtils,
    IConversionContextStack<Map, ACG.Unit> contextStack
  )
  {
    _gisGeometryConverter = gisGeometryConverter;
    _fieldsUtils = fieldsUtils;
    _featureClassUtils = featureClassUtils;
    _arcGISProjectUtils = arcGISProjectUtils;
    _contextStack = contextStack;
  }

  public List<(string parentPath, string converted)> WriteGeometriesToDatasets(
    Dictionary<string, (string parentPath, ACG.Geometry geom, string? parentId)> convertedObjs
  )
  {
    List<(string, string)> result = new();
    // 1. Sort features into groups by path and geom type
    Dictionary<string, (List<ACG.Geometry> geometries, string? parentId)> geometryGroups = new();
    foreach (var item in convertedObjs)
    {
      try
      {
        string objId = item.Key;
        (string parentPath, ACG.Geometry geom, string? parentId) = item.Value;

        // add dictionnary item if doesn't exist yet
        if (!geometryGroups.TryGetValue(parentPath, out var value))
        {
          value = (new List<ACG.Geometry>(), parentId);
          geometryGroups[parentPath] = value;
        }

        value.geometries.Add(geom);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        // POC: report, etc.
        Debug.WriteLine($"conversion error happened. {ex.Message}");
      }
    }

    // 2. for each group create a Dataset and add geometries there as Features
    foreach (var item in geometryGroups)
    {
      try
      {
        string parentPath = item.Key;
        (List<ACG.Geometry> geomList, string? parentId) = item.Value;
        ACG.GeometryType geomType = geomList[0].GeometryType;
        try
        {
          string converted = CreateDatasetInDatabase(geomType, geomList, parentId);
          result.Add((parentPath, converted));
        }
        catch (GeodatabaseGeometryException)
        {
          // do nothing if conversion of some geometry groups fails
        }
      }
      catch (Exception e) when (!e.IsFatal())
      {
        // POC: report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }
    return result;
  }

  private string CreateDatasetInDatabase(ACG.GeometryType geomType, List<ACG.Geometry> geomList, string? parentId)
  {
    string databasePath = _arcGISProjectUtils.GetDatabasePath();
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(databasePath));
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // get Spatial Reference from the document
    ACG.SpatialReference spatialRef = _contextStack.Current.Document.SpatialReference;

    // TODO: create Fields
    List<FieldDescription> fields = new(); // _fieldsUtils.GetFieldsFromSpeckleLayer(target);

    // TODO: generate meaningful name
    string featureClassName = $"speckleID_{geomType}_{parentId}";

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

    FeatureClass newFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);
    // Add features to the FeatureClass
    geodatabase.ApplyEdits(() =>
    {
      _featureClassUtils.AddNonGISFeaturesToFeatureClass(newFeatureClass, geomList, fields);
    });

    return featureClassName;
  }
}

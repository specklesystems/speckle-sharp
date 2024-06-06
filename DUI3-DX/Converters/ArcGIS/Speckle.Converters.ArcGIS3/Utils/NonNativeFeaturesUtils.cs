using System.Diagnostics;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using Speckle.Converters.Common;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

public class NonNativeFeaturesUtils : INonNativeFeaturesUtils
{
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public NonNativeFeaturesUtils(
    IFeatureClassUtils featureClassUtils,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _featureClassUtils = featureClassUtils;
    _contextStack = contextStack;
  }

  public List<(string parentPath, string converted)> WriteGeometriesToDatasets(
    Dictionary<string, (string parentPath, ACG.Geometry geom, string? parentId, Base baseObj)> convertedObjs,
    List<(
      bool isGisLayer,
      bool status,
      Base obj,
      string? datasetId,
      int? rowIndexNonGis,
      Exception? exception
    )> resultTracker
  )
  {
    List<(string, string)> result = new();
    bool isGisCurrent = false;

    // 1. Sort features into groups by path and geom type
    Dictionary<string, (List<ACG.Geometry> geometries, string? parentId, List<Base> baseObjs)> geometryGroups = new();
    foreach (var item in convertedObjs)
    {
      try
      {
        string objId = item.Key;
        (string parentPathForLayerNesting, ACG.Geometry geom, string? parentId, Base baseObj) = item.Value;

        // add dictionnary item if doesn't exist yet
        // Key must be unique per parent and speckle_type
        // Key is composed of parentId and parentPath (that contains speckle_type)
        string uniqueKey = $"{parentId}_{parentPathForLayerNesting}";
        if (!geometryGroups.TryGetValue(uniqueKey, out (List<ACG.Geometry>, string?, List<Base>) value))
        {
          geometryGroups[uniqueKey] = (new List<ACG.Geometry>(), parentId, new List<Base>());
        }

        geometryGroups[uniqueKey].geometries.Add(geom);
        geometryGroups[uniqueKey].baseObjs.Add(baseObj);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        resultTracker.Add((isGisCurrent, false, item.Value.baseObj, null, null, ex));
        // POC: report, etc.
        Debug.WriteLine($"conversion error happened. {ex.Message}");
      }
    }

    // 2. for each group create a Dataset and add geometries there as Features
    foreach (var item in geometryGroups)
    {
      try
      {
        (List<ACG.Geometry> geometries, string? parentId, List<Base> baseObjs) = item.Value;
        string uniqueKey = item.Key; // parentId_parentPathForLayerNesting
        string parentPathForLayerNesting = uniqueKey.Split('_', 2)[^1];
        string speckle_type = parentPathForLayerNesting.Split("\\")[^1];
        try
        {
          string datasetId = CreateDatasetInDatabase(speckle_type, geometries, parentId);
          result.Add((parentPathForLayerNesting, datasetId));

          int rowIndex = 0;
          foreach (Base baseObj in item.Value.baseObjs)
          {
            resultTracker.Add((isGisCurrent, true, baseObj, datasetId, rowIndex, null));
            rowIndex += 1;
          }
        }
        catch (GeodatabaseGeometryException ex)
        {
          foreach (Base baseObj in item.Value.baseObjs)
          {
            resultTracker.Add((isGisCurrent, false, baseObj, null, null, ex));
          }
          // do nothing if conversion of some geometry groups fails
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        foreach (Base baseObj in item.Value.baseObjs)
        {
          resultTracker.Add((isGisCurrent, false, baseObj, null, null, ex));
        }
        // POC: report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }
    return result;
  }

  private string CreateDatasetInDatabase(string speckle_type, List<ACG.Geometry> geomList, string? parentId)
  {
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath =
      new(_contextStack.Current.Document.SpeckleDatabasePath);
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // get Spatial Reference from the document
    ACG.SpatialReference spatialRef = _contextStack.Current.Document.Map.SpatialReference;

    // TODO: create Fields
    List<FieldDescription> fields = new(); // _fieldsUtils.GetFieldsFromSpeckleLayer(target);

    // TODO: generate meaningful name
    string featureClassName = $"speckleTYPE_{speckle_type}_speckleID_{parentId}";

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
      ACG.GeometryType geomType = geomList[0].GeometryType;
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

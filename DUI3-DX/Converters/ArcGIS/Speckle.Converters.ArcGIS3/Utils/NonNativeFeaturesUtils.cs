using System.Diagnostics;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using Speckle.Converters.Common;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using Speckle.Core.Logging;
using Speckle.Core.Models.GraphTraversal;

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

  public void WriteGeometriesToDatasets(
    // Dictionary<TraversalContext, (string nestedParentPath, ACG.Geometry geom)> conversionTracker
    Dictionary<TraversalContext, ObjectConversionTracker> conversionTracker
  )
  {
    // 1. Sort features into groups by path and geom type
    Dictionary<string, List<ACG.Geometry>> geometryGroups = new();
    foreach (var item in conversionTracker)
    {
      try
      {
        ACG.Geometry? geom = item.Value.HostAppGeom;
        string? datasetId = item.Value.DatasetId;
        if (geom != null && datasetId == null) // only non-native geomerties, not written into a dataset yet
        {
          string nestedParentPath = item.Value.NestedLayerName;
          string speckle_type = nestedParentPath.Split('\\')[^1];

          TraversalContext context = item.Key;
          string? parentId = context.Parent?.Current.id;

          // add dictionnary item if doesn't exist yet
          // Key must be unique per parent and speckle_type
          string uniqueKey = $"speckleTYPE_{speckle_type}_speckleID_{parentId}";
          if (!geometryGroups.TryGetValue(uniqueKey, out _))
          {
            geometryGroups[uniqueKey] = new List<ACG.Geometry>();
          }

          geometryGroups[uniqueKey].Add(geom);

          // record changes in conversion tracker
          conversionTracker[item.Key].AddDatasetId(uniqueKey);
          conversionTracker[item.Key].AddDatasetRow(geometryGroups[uniqueKey].Count - 1);
        }
        else
        {
          throw new ArgumentException($"Unexpected geometry and datasetId values: {geom}, {datasetId}");
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        // POC: report, etc.
        conversionTracker[item.Key].AddException(ex);
        Debug.WriteLine($"conversion error happened. {ex.Message}");
      }
    }

    // 2. for each group create a Dataset and add geometries there as Features
    foreach (var item in geometryGroups)
    {
      string uniqueKey = item.Key;
      List<ACG.Geometry> geomList = item.Value;
      try
      {
        CreateDatasetInDatabase(uniqueKey, geomList);
      }
      catch (GeodatabaseGeometryException ex)
      {
        // do nothing if writing of some geometry groups fails
        // record in a conversionTracker:
        foreach (var conversionItem in conversionTracker)
        {
          if (conversionItem.Value.DatasetId == uniqueKey)
          {
            conversionTracker[conversionItem.Key].AddException(ex);
          }
        }
      }
    }
  }

  private void CreateDatasetInDatabase(string featureClassName, List<ACG.Geometry> geomList)
  {
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath =
      new(_contextStack.Current.Document.SpeckleDatabasePath);
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // get Spatial Reference from the document
    ACG.SpatialReference spatialRef = _contextStack.Current.Document.Map.SpatialReference;

    // TODO: create Fields
    List<FieldDescription> fields = new(); // _fieldsUtils.GetFieldsFromSpeckleLayer(target);

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
  }
}

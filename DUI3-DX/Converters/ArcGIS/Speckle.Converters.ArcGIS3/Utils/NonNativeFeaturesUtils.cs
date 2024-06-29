using System.Diagnostics;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using Speckle.Converters.Common;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using Speckle.Core.Logging;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

public class NonNativeFeaturesUtils : INonNativeFeaturesUtils
{
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;
  private readonly IArcGISFieldUtils _fieldUtils;

  public NonNativeFeaturesUtils(
    IFeatureClassUtils featureClassUtils,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack,
    IArcGISFieldUtils fieldUtils
  )
  {
    _featureClassUtils = featureClassUtils;
    _contextStack = contextStack;
    _fieldUtils = fieldUtils;
  }

  public void WriteGeometriesToDatasets(
    // Dictionary<TraversalContext, (string nestedParentPath, ACG.Geometry geom)> conversionTracker
    Dictionary<TraversalContext, ObjectConversionTracker> conversionTracker
  )
  {
    // 1. Sort features into groups by path and geom type
    Dictionary<string, List<(Base baseObj, ACG.Geometry convertedGeom)>> geometryGroups = new();
    foreach (var item in conversionTracker)
    {
      try
      {
        TraversalContext context = item.Key;
        var trackerItem = item.Value;
        ACG.Geometry? geom = trackerItem.HostAppGeom;
        string? datasetId = trackerItem.DatasetId;
        if (geom != null && datasetId == null) // only non-native geomerties, not written into a dataset yet
        {
          string speckle_type = trackerItem.Base.speckle_type.Split(".")[^1];
          string? parentId = context.Parent?.Current.id;

          // add dictionnary item if doesn't exist yet
          // Key must be unique per parent and speckle_type
          string uniqueKey = $"speckleTYPE_{speckle_type}_speckleID_{parentId}";
          if (!geometryGroups.TryGetValue(uniqueKey, out _))
          {
            geometryGroups[uniqueKey] = new();
          }

          geometryGroups[uniqueKey].Add((trackerItem.Base, geom));

          // record changes in conversion tracker
          trackerItem.AddDatasetId(uniqueKey);
          trackerItem.AddDatasetRow(geometryGroups[uniqueKey].Count - 1);
          conversionTracker[item.Key] = trackerItem;
        }
        else if (geom == null && datasetId != null) // GIS layers, already written to a dataset
        {
          continue;
        }
        else
        {
          throw new ArgumentException($"Unexpected geometry and datasetId values: {geom}, {datasetId}");
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        // POC: report, etc.
        var trackerItem = item.Value;
        trackerItem.AddException(ex);
        conversionTracker[item.Key] = trackerItem;
        Debug.WriteLine($"conversion error happened. {ex.Message}");
      }
    }

    // 2. for each group create a Dataset and add geometries there as Features
    foreach (var item in geometryGroups)
    {
      string uniqueKey = item.Key;
      List<(Base, ACG.Geometry)> listOfGeometryTuples = item.Value;
      try
      {
        CreateDatasetInDatabase(uniqueKey, listOfGeometryTuples);
      }
      catch (GeodatabaseGeometryException ex)
      {
        // do nothing if writing of some geometry groups fails
        // only record in conversionTracker:
        foreach (var conversionItem in conversionTracker)
        {
          if (conversionItem.Value.DatasetId == uniqueKey)
          {
            var trackerItem = conversionItem.Value;
            trackerItem.AddException(ex);
            conversionTracker[conversionItem.Key] = trackerItem;
          }
        }
      }
    }
  }

  private void CreateDatasetInDatabase(
    string featureClassName,
    List<(Base baseObj, ACG.Geometry convertedGeom)> listOfGeometryTuples
  )
  {
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath =
      new(_contextStack.Current.Document.SpeckleDatabasePath);
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // get Spatial Reference from the Active CRS for Receive
    ACG.SpatialReference spatialRef = _contextStack.Current.Document.ActiveCRSoffsetRotation.SpatialReference;

    // create Fields
    List<(FieldDescription, Func<Base, object?>)> fieldsAndFunctions = _fieldUtils.CreateFieldsFromListOfBase(
      listOfGeometryTuples.Select(x => x.baseObj).ToList()
    );

    // delete FeatureClass if already exists
    try
    {
      FeatureClassDefinition fClassDefinition = geodatabase.GetDefinition<FeatureClassDefinition>(featureClassName);
      FeatureClassDescription existingDescription = new(fClassDefinition);
      schemaBuilder.Delete(existingDescription);
      schemaBuilder.Build();
    }
    catch (Exception ex) when (!ex.IsFatal()) //(GeodatabaseTableException)
    {
      // "The table was not found."
      // delete Table if already exists
      try
      {
        TableDefinition fClassDefinition = geodatabase.GetDefinition<TableDefinition>(featureClassName);
        TableDescription existingDescription = new(fClassDefinition);
        schemaBuilder.Delete(existingDescription);
        schemaBuilder.Build();
      }
      catch (Exception ex2) when (!ex2.IsFatal()) //(GeodatabaseTableException)
      {
        // "The table was not found.", do nothing
      }
    }

    // Create FeatureClass
    try
    {
      // POC: make sure class has a valid crs
      ACG.GeometryType geomType = listOfGeometryTuples[0].convertedGeom.GeometryType;
      ShapeDescription shpDescription = new(geomType, spatialRef) { HasZ = true };
      FeatureClassDescription featureClassDescription =
        new(featureClassName, fieldsAndFunctions.Select(x => x.Item1), shpDescription);
      FeatureClassToken featureClassToken = schemaBuilder.Create(featureClassDescription);
    }
    catch (ArgumentException ex)
    {
      // if name has invalid characters/combinations
      // or 'The table contains multiple fields with the same name.:
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
      _featureClassUtils.AddNonGISFeaturesToFeatureClass(newFeatureClass, listOfGeometryTuples, fieldsAndFunctions);
    });
  }
}

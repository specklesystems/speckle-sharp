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
  private readonly IRawConversion<IReadOnlyList<Base>, ACG.Geometry> _gisGeometryConverter;
  private readonly IArcGISFieldUtils _fieldsUtils;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;

  public NonNativeFeaturesUtils(
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter,
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

  public List<Tuple<string, string>> WriteGeometriesToDatasets(
    Dictionary<string, Tuple<List<string>, ACG.Geometry>> target
  )
  {
    List<Tuple<string, string>> result = new();
    try
    {
      // 1. Sort features into groups by path and geom type
      Dictionary<string, List<ACG.Geometry>> geometryGroups = new();
      foreach (var item in target)
      {
        string objId = item.Key;
        (List<string> objPath, ACG.Geometry geom) = item.Value;
        string geomType = objPath[^1];
        string parentPath = $"{string.Join("\\", objPath.Where((x, i) => i < objPath.Count - 1))}\\{geomType}";

        // add dictionnary item if doesn't exist yet
        if (!geometryGroups.ContainsKey(parentPath))
        {
          geometryGroups[parentPath] = new List<ACG.Geometry>();
        }
        geometryGroups[parentPath].Add(geom);
      }

      // 2. for each group create a Dataset and add geometries there as Features
      foreach ((string parentPath, List<ACG.Geometry> geomList) in geometryGroups)
      {
        ACG.GeometryType geomType = _featureClassUtils.GetGeometryTypeFromString(parentPath.Split("\\")[^1]);
        try
        {
          string converted = CreateDatasetInDatabase(geomType, geomList);
          result.Add(Tuple.Create(parentPath, converted));
        }
        catch (GeodatabaseGeometryException)
        {
          // do nothing if conversion of some geometry groups fails
        }
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // POC: report, etc.
      Debug.WriteLine("conversion error happened.");
    }
    return result;
  }

  private string CreateDatasetInDatabase(ACG.GeometryType geomType, List<ACG.Geometry> geomList)
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
    string featureClassName = "hash_" + Utilities.HashString(string.Join("\\", geomList.Select(x => x.GetHashCode())));

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

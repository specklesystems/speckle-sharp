using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Data;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common.Objects;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Layers;

public class TableLayerToHostConverter : ITypedConverter<VectorLayer, Table>
{
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISFieldUtils _fieldsUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;

  public TableLayerToHostConverter(
    IFeatureClassUtils featureClassUtils,
    IArcGISProjectUtils arcGISProjectUtils,
    IArcGISFieldUtils fieldsUtils
  )
  {
    _featureClassUtils = featureClassUtils;
    _arcGISProjectUtils = arcGISProjectUtils;
    _fieldsUtils = fieldsUtils;
  }

  public Table Convert(VectorLayer target)
  {
    string databasePath = _arcGISProjectUtils.GetDatabasePath();
    FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(databasePath));
    Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
    SchemaBuilder schemaBuilder = new(geodatabase);

    // create Fields
    List<FieldDescription> fields = _fieldsUtils.GetFieldsFromSpeckleLayer(target);

    // getting rid of forbidden symbols in the class name: adding a letter in the beginning
    // https://pro.arcgis.com/en/pro-app/3.1/tool-reference/tool-errors-and-warnings/001001-010000/tool-errors-and-warnings-00001-00025-000020.htm
    string featureClassName = "speckleID_" + target.id;

    // delete FeatureClass if already exists
    foreach (TableDefinition fClassDefinition in geodatabase.GetDefinitions<TableDefinition>())
    {
      // will cause GeodatabaseCatalogDatasetException if doesn't exist in the database
      if (fClassDefinition.GetName() == featureClassName)
      {
        TableDescription existingDescription = new(fClassDefinition);
        schemaBuilder.Delete(existingDescription);
        schemaBuilder.Build();
      }
    }

    // Create Table
    try
    {
      TableDescription featureClassDescription = new(featureClassName, fields);
      TableToken featureClassToken = schemaBuilder.Create(featureClassDescription);
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
      Table newFeatureClass = geodatabase.OpenDataset<Table>(featureClassName);
      // Add features to the FeatureClass
      List<GisFeature> gisFeatures = target.elements.Select(x => (GisFeature)x).ToList();
      geodatabase.ApplyEdits(() =>
      {
        _featureClassUtils.AddFeaturesToTable(newFeatureClass, gisFeatures, fields);
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

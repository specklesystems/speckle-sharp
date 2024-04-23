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

[NameAndRankValue(nameof(VectorLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<VectorLayer, string>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<Base, ACG.Geometry> _gisGeometryConverter;
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;

  public VectorLayerToHostConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<Base, ACG.Geometry> gisGeometryConverter,
    IFeatureClassUtils featureClassUtils,
    IArcGISProjectUtils arcGISProjectUtils
  )
  {
    _contextStack = contextStack;
    _gisGeometryConverter = gisGeometryConverter;
    _featureClassUtils = featureClassUtils;
    _arcGISProjectUtils = arcGISProjectUtils;
  }

  public object Convert(Base target) => RawConvert((VectorLayer)target);

  private const string FID_FIELD_NAME = "OBJECTID";

  public string RawConvert(VectorLayer target)
  {
    try
    {
      // POC: define the best place to start QueuedTask (entire receive or per converter)
      string databasePath = _arcGISProjectUtils.GetDatabasePath();
      FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(databasePath));
      Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
      SchemaBuilder schemaBuilder = new(geodatabase);

      // getting rid of forbidden symbols in the class name:
      // https://pro.arcgis.com/en/pro-app/3.1/tool-reference/tool-errors-and-warnings/001001-010000/tool-errors-and-warnings-00001-00025-000020.htm
      string featureClassName = target.id;
      //  $"{target.id}___{target.name.Replace(" ", "_").Replace("%", "_").Replace("*", "_")}";

      string wktString = string.Empty;
      if (target.crs is not null && target.crs.wkt is not null)
      {
        wktString = target.crs.wkt.ToString();
      }
      SpatialReference spatialRef = SpatialReferenceBuilder.CreateSpatialReference(wktString);

      GeometryType geomType = _featureClassUtils.GetLayerGeometryType(target);

      // Create FeatureClass
      List<FieldDescription> fields = new();
      List<string> fieldAdded = new();
      foreach (var field in target.attributes.GetMembers(DynamicBaseMemberType.Dynamic))
      {
        if (!fieldAdded.Contains(field.Key) && field.Key != FID_FIELD_NAME)
        {
          // POC: TODO: choose the right type for Field
          // TODO check for the forbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588

          // POC: assemble constants in a shared place
          // fields.Add(new FieldDescription(field, FieldType.Integer));
          fields.Add(FieldDescription.CreateStringField(field.Key, 255)); // (int)(long)target.attributes[field.Value]));
          fieldAdded.Add(field.Key);
        }
      }
      try
      {
        FeatureClassDescription featureClassDescription =
          new(featureClassName, fields, new ShapeDescription(geomType, spatialRef));
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

      // Add features to the FeatureClass
      FeatureClass newFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);
      // TODO: repeat for other geometry types
      if (geomType == GeometryType.Multipoint)
      {
        geodatabase.ApplyEdits(() =>
        {
          _featureClassUtils.AddFeaturesToFeatureClass(newFeatureClass, target, fieldAdded, _gisGeometryConverter);
        });
      }
      return featureClassName;
    }
    catch (GeodatabaseException exObj)
    {
      // POC: review the exception
      throw new InvalidOperationException($"Something went wrong: {exObj.Message}");
    }
  }
}

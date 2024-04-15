using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(VectorLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<VectorLayer, Task<string>>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<Base, ArcGIS.Core.Geometry.Geometry> _gisGeometryConverter;

  public VectorLayerToHostConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<Base, ArcGIS.Core.Geometry.Geometry> gisGeometryConverter
  )
  {
    _contextStack = contextStack;
    _gisGeometryConverter = gisGeometryConverter;
  }

  public object Convert(Base target) => RawConvert((VectorLayer)target);

  public Task<string> RawConvert(VectorLayer target)
  {
    try
    {
      return QueuedTask.Run(() =>
      {
        var projectUtils = new ArcGISProjectUtils();
        var utils = new FeatureClassUtils();

        string databasePath = projectUtils.GetDatabasePath();
        FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath = new(new Uri(databasePath));
        Geodatabase geodatabase = new(fileGeodatabaseConnectionPath);
        SchemaBuilder schemaBuilder = new(geodatabase);

        // https://pro.arcgis.com/en/pro-app/3.1/tool-reference/tool-errors-and-warnings/001001-010000/tool-errors-and-warnings-00001-00025-000020.htm
        string featureClassName = $"{target.id}___{target.name.Replace(" ", "_").Replace("%", "_").Replace("*", "_")}";

        string wktString = string.Empty;
        if (target.crs is not null && target.crs.wkt is not null)
        {
          wktString = target.crs.wkt.ToString();
        }
        SpatialReference spatialRef = SpatialReferenceBuilder.CreateSpatialReference(wktString);

        GeometryType geomType = utils.GetLayerGeometryType(target);

        // Create FeatureClass
        List<FieldDescription> fields = new();
        List<string> fieldAdded = new();
        foreach (var field in target.attributes.GetMembers(DynamicBaseMemberType.Dynamic))
        {
          if (!fieldAdded.Contains(field.Key) && field.Key != "OBJECTID")
          {
            // TODO: choose the right type for Field
            // TODO check for the frbidden characters/combinations: https://support.esri.com/en-us/knowledge-base/what-characters-should-not-be-used-in-arcgis-for-field--000005588

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
          IReadOnlyList<string> errors = schemaBuilder.ErrorMessages;
        }

        // Add features to the FeatureClass
        FeatureClass newFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName);
        // TODO: repeat for other geometry types
        if (geomType == GeometryType.Multipoint)
        {
          geodatabase.ApplyEdits(() =>
          {
            utils.AddFeaturesToFeatureClass(newFeatureClass, target, fieldAdded, _gisGeometryConverter);
          });
        }
        return featureClassName;
      });
    }
    catch (GeodatabaseException exObj)
    {
      throw new InvalidOperationException($"Something went wrong: {exObj.Message}");
    }
  }
}

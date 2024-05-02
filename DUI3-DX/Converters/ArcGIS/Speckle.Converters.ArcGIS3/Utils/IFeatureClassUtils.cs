using ArcGIS.Core.Data;
using Objects.GIS;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface IFeatureClassUtils
{
  void AddFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    VectorLayer target,
    List<string> fieldAdded,
    IRawConversion<Base, ACG.Geometry> gisGeometryConverter
  );
  void AddFeaturesToTable(Table newFeatureClass, List<GisFeature> gisFeatures, List<FieldDescription> fields);
  public string CleanCharacters(string key);
  public RowBuffer AssignFieldValuesToRow(RowBuffer rowBuffer, List<FieldDescription> fields, GisFeature feat);
  public object? FieldValueToNativeType(FieldType fieldType, object? value);
  public List<FieldDescription> GetFieldsFromSpeckleLayer(VectorLayer target);
  public List<FieldDescription> GetFieldsFromGeometryList(List<Base> target);
  public FieldType GetFieldTypeFromInt(int fieldType);
  public ACG.GeometryType GetLayerGeometryType(VectorLayer target);
}

using ArcGIS.Core.Data;
using Objects.GIS;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface IFeatureClassUtils
{
  void AddFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    List<GisFeature> gisFeatures,
    List<FieldDescription> fields,
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter
  );
  public object? FieldValueToNativeType(FieldType fieldType, object? value);
  public List<FieldDescription> GetFieldsFromSpeckleLayer(VectorLayer target);
  public FieldType GetFieldTypeFromInt(int fieldType);
  public ACG.GeometryType GetLayerGeometryType(VectorLayer target);
}

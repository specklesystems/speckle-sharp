using ArcGIS.Core.Data;
using Objects.GIS;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface IFeatureClassUtils
{
  void AddFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    List<GisFeature> gisFeatures,
    List<string> fieldAdded,
    IRawConversion<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter
  );
  public FieldType GetFieldTypeFromInt(int fieldType);
  public ACG.GeometryType GetLayerGeometryType(VectorLayer target);
}

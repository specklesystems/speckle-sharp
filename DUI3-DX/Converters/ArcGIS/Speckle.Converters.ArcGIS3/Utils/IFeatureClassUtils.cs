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
    ITypedConverter<IReadOnlyList<Base>, ACG.Geometry> gisGeometryConverter
  );
  void AddNonGISFeaturesToFeatureClass(
    FeatureClass newFeatureClass,
    List<(Base baseObj, ACG.Geometry convertedGeom)> featuresTuples,
    List<(FieldDescription, Func<Base, object?>)> fieldsAndFunctions
  );
  void AddFeaturesToTable(Table newFeatureClass, List<GisFeature> gisFeatures, List<FieldDescription> fields);
}

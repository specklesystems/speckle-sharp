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
    IRawConversion<Base, ACG.Geometry> gisGeometryConverter
  );

  ACG.GeometryType GetLayerGeometryType(VectorLayer target);
}

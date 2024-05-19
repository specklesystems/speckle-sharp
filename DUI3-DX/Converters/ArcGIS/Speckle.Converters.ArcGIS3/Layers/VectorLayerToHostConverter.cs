using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Layers;

[NameAndRankValue(nameof(VectorLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<VectorLayer, string>
{
  private readonly IRawConversion<VectorLayer, FeatureClass> _featureClassConverter;
  private readonly IRawConversion<VectorLayer, Table> _tableConverter;
  private readonly IRawConversion<VectorLayer, LasDatasetLayer> _pointcloudLayerConverter;
  private readonly IFeatureClassUtils _featureClassUtils;

  public VectorLayerToHostConverter(
    IRawConversion<VectorLayer, FeatureClass> featureClassConverter,
    IRawConversion<VectorLayer, Table> tableConverter,
    IRawConversion<VectorLayer, LasDatasetLayer> pointcloudLayerConverter,
    IFeatureClassUtils featureClassUtils
  )
  {
    _featureClassConverter = featureClassConverter;
    _tableConverter = tableConverter;
    _pointcloudLayerConverter = pointcloudLayerConverter;
    _featureClassUtils = featureClassUtils;
  }

  public object Convert(Base target) => RawConvert((VectorLayer)target);

  public string RawConvert(VectorLayer target)
  {
    // pointcloud layers need to be checked separately, because there is no ArcGIS Geometry type
    // for Pointcloud. In ArcGIS it's a completely different layer class, so "GetLayerGeometryType"
    // will return "Invalid" type
    if (target.geomType == GISLayerGeometryType.POINTCLOUD)
    {
      return _pointcloudLayerConverter.RawConvert(target).Name;
    }

    // check if Speckle VectorLayer should become a FeatureClass, StandaloneTable or PointcloudLayer
    GeometryType geomType = _featureClassUtils.GetLayerGeometryType(target);
    if (geomType != GeometryType.Unknown) // feature class
    {
      return _featureClassConverter.RawConvert(target).GetName();
    }
    else // table
    {
      return _tableConverter.RawConvert(target).GetName();
    }
  }
}

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.ToHost.TopLevel;

[NameAndRankValue(nameof(VectorLayer), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class VectorLayerToHostConverter : IToHostTopLevelConverter, ITypedConverter<VectorLayer, string>
{
  private readonly ITypedConverter<VectorLayer, FeatureClass> _featureClassConverter;
  private readonly ITypedConverter<VectorLayer, Table> _tableConverter;
  private readonly ITypedConverter<VectorLayer, LasDatasetLayer> _pointcloudLayerConverter;
  private readonly IFeatureClassUtils _featureClassUtils;

  public VectorLayerToHostConverter(
    ITypedConverter<VectorLayer, FeatureClass> featureClassConverter,
    ITypedConverter<VectorLayer, Table> tableConverter,
    ITypedConverter<VectorLayer, LasDatasetLayer> pointcloudLayerConverter,
    IFeatureClassUtils featureClassUtils
  )
  {
    _featureClassConverter = featureClassConverter;
    _tableConverter = tableConverter;
    _pointcloudLayerConverter = pointcloudLayerConverter;
    _featureClassUtils = featureClassUtils;
  }

  public object Convert(Base target) => Convert((VectorLayer)target);

  public string Convert(VectorLayer target)
  {
    // pointcloud layers need to be checked separately, because there is no ArcGIS Geometry type
    // for Pointcloud. In ArcGIS it's a completely different layer class, so "GetLayerGeometryType"
    // will return "Invalid" type
    if (target.geomType == GISLayerGeometryType.POINTCLOUD)
    {
      return _pointcloudLayerConverter.Convert(target).Name;
    }

    // check if Speckle VectorLayer should become a FeatureClass, StandaloneTable or PointcloudLayer
    ACG.GeometryType geomType = _featureClassUtils.GetLayerGeometryType(target);
    if (geomType != ACG.GeometryType.Unknown) // feature class
    {
      return _featureClassConverter.Convert(target).GetName();
    }
    else // table
    {
      return _tableConverter.Convert(target).GetName();
    }
  }
}

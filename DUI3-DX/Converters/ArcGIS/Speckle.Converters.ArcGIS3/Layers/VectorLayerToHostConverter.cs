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
  private readonly IRawConversion<VectorLayer, StandaloneTable> _tableConverter;
  private readonly IRawConversion<VectorLayer, LasDatasetLayer> _pointcloudLayerConverter;
  private readonly IFeatureClassUtils _featureClassUtils;

  public VectorLayerToHostConverter(
    IRawConversion<VectorLayer, FeatureClass> featureClassConverter,
    IRawConversion<VectorLayer, StandaloneTable> tableConverter,
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
    // check if Speckle VectorLayer should become a FeatureClass, StandaloneTable or PointcloudLayer
    GeometryType geomType = _featureClassUtils.GetLayerGeometryType(target);
    if (geomType != GeometryType.Unknown) // feature class
    {
      return _featureClassConverter.RawConvert(target).GetName();
    }
    if (target.geomType == "None") // table
    {
      return _tableConverter.RawConvert(target).Name;
    }
    if (target.geomType == "Pointcloud") // table
    {
      return _pointcloudLayerConverter.RawConvert(target).Name;
    }

    throw new SpeckleConversionException($"Unknown geometry type for layer {target.name}");
  }
}

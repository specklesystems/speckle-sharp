using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.ArcGIS3.Utils;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Layers;

public class TableLayerToHostConverter : IRawConversion<VectorLayer, StandaloneTable>
{
  private readonly IFeatureClassUtils _featureClassUtils;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;

  public TableLayerToHostConverter(IFeatureClassUtils featureClassUtils, IArcGISProjectUtils arcGISProjectUtils)
  {
    _featureClassUtils = featureClassUtils;
    _arcGISProjectUtils = arcGISProjectUtils;
  }

  public object Convert(Base target) => RawConvert((VectorLayer)target);

  // private const string FID_FIELD_NAME = "OBJECTID";

  public StandaloneTable RawConvert(VectorLayer target)
  {
    throw new InvalidOperationException($"Something went wrong");
  }
}

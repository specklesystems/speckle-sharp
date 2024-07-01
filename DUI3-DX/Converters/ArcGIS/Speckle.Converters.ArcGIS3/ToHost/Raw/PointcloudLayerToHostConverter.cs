using ArcGIS.Desktop.Mapping;
using Objects.GIS;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.ToHost.Raw;

public class PointcloudLayerToHostConverter : ITypedConverter<VectorLayer, LasDatasetLayer>
{
  public object Convert(Base target) => Convert((VectorLayer)target);

  public LasDatasetLayer Convert(VectorLayer target)
  {
    // POC:
    throw new NotImplementedException($"Receiving Pointclouds is not supported");
  }
}

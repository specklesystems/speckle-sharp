using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(RG.PointCloud), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RhinoPointCloudToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.PointCloud, SOG.Pointcloud>
{
  public RhinoPointCloudToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.PointCloud, SOG.Pointcloud> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}

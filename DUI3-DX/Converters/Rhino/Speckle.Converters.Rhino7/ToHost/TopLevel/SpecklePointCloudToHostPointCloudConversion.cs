using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Pointcloud), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpecklePointCloudToHostPointCloudConversion
  : SpeckleToHostGeometryBaseConversion<SOG.Pointcloud, RG.PointCloud>
{
  public SpecklePointCloudToHostPointCloudConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Pointcloud, RG.PointCloud> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}

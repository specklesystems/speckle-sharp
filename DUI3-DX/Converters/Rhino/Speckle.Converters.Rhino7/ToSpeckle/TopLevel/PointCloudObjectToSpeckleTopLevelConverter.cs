using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(PointCloudObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointCloudObjectToSpeckleTopLevelTopLevelConverter
  : RhinoObjectToSpeckleTopLevelConverter<PointCloudObject, RG.PointCloud, SOG.Pointcloud>
{
  public PointCloudObjectToSpeckleTopLevelTopLevelConverter(IRawConversion<RG.PointCloud, SOG.Pointcloud> conversion)
    : base(conversion) { }

  protected override RG.PointCloud GetTypedGeometry(PointCloudObject input) => input.PointCloudGeometry;
}

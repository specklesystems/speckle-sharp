using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(IRhinoPointCloudObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointCloudObjectToSpeckleTopLevelConverter
  : RhinoObjectToSpeckleTopLevelConverter<IRhinoPointCloudObject, IRhinoPointCloud, SOG.Pointcloud>
{
  public PointCloudObjectToSpeckleTopLevelConverter(ITypedConverter<IRhinoPointCloud, SOG.Pointcloud> conversion)
    : base(conversion) { }

  protected override IRhinoPointCloud GetTypedGeometry(IRhinoPointCloudObject input) => input.PointCloudGeometry;
}

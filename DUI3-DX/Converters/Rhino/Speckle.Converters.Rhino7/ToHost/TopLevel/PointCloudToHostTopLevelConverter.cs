using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Pointcloud), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointCloudToHostTopLevelConverter
  : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Pointcloud, IRhinoPointCloud>
{
  public PointCloudToHostTopLevelConverter(
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<SOG.Pointcloud, IRhinoPointCloud> geometryBaseConverter,
    IRhinoTransformFactory rhinoTransformFactory
  )
    : base(contextStack, geometryBaseConverter, rhinoTransformFactory) { }
}

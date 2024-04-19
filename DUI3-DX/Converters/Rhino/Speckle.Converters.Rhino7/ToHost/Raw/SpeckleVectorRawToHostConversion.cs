using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleVectorRawToHostConversion : IRawConversion<SOG.Vector, RG.Vector3d>
{
  public RG.Vector3d RawConvert(SOG.Vector target) => new(target.x, target.y, target.z);
}

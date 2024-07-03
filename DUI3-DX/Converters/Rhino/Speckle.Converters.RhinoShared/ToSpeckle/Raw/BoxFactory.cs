using Speckle.InterfaceGenerator;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

[GenerateAutoInterface]
public class BoxFactory : IBoxFactory
{
  public RG.Box Create(RG.BoundingBox bb) => new(bb);
}

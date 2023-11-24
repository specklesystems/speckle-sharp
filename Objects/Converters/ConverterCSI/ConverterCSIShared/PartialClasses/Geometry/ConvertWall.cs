using Objects.Structural.Geometry;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public Element2D WallToSpeckle(string name)
  {
    return AreaToSpeckle(name);
  }
}

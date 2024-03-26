using Objects.Structural.Geometry;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public Element2D FloorToSpeckle(string name)
  {
    return AreaToSpeckle(name);
  }
}

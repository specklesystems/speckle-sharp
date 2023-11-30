using Objects.Structural.Geometry;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public Element1D ColumnToSpeckle(string name)
  {
    return FrameToSpeckle(name);
  }
}

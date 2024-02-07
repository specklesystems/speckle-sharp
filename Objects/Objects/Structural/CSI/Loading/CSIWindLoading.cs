using Objects.Structural.CSI.Analysis;
using Objects.Structural.Loading;

namespace Objects.Structural.CSI.Loading;

public class CSIWindLoadingFace : LoadFace
{
  public double Cp { get; set; }

  public WindPressureType WindPressureType { get; set; }
}

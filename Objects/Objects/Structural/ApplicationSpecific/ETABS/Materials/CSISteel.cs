using Objects.Structural.Materials;

namespace Objects.Structural.CSI.Materials;

public class CSISteel : Steel
{
  public int SSHysType { get; set; }
  public int SSType { get; set; }
  public double EFy { get; set; }
  public double EFu { get; set; }
  public double strainAtMaxStress { get; set; }
  public double strainAtHardening { get; set; }
}

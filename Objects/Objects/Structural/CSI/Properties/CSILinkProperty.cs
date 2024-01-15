using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.CSI.Properties;

public class CSILinkProperty : Property1D
{
  [SchemaInfo("CSILink", "Create an CSI Link Property", "CSI", "Properties")]
  public CSILinkProperty(
    string name,
    double mass,
    double weight,
    double rotationalInertia1,
    double rotationalInertia2,
    double rotationalInertia3,
    double m2PdeltaEnd1,
    double mP2deltaEnd2,
    double mP3deltaEnd1,
    double mP3deltaEnd2
  )
  {
    this.name = name;
    this.mass = mass;
    this.weight = weight;
    this.rotationalInertia1 = rotationalInertia1;
    this.rotationalInertia2 = rotationalInertia2;
    this.rotationalInertia3 = rotationalInertia3;
    M2PdeltaEnd1 = m2PdeltaEnd1;
    MP2deltaEnd2 = mP2deltaEnd2;
    MP3deltaEnd1 = mP3deltaEnd1;
    MP3deltaEnd2 = mP3deltaEnd2;
  }

  public CSILinkProperty() { }

  public double mass { get; set; }
  public double weight { get; set; }
  public double rotationalInertia1 { get; set; }
  public double rotationalInertia2 { get; set; }
  public double rotationalInertia3 { get; set; }
  public double M2PdeltaEnd1 { get; set; }
  public double MP2deltaEnd2 { get; set; }
  public double MP3deltaEnd1 { get; set; }
  public double MP3deltaEnd2 { get; set; }
}

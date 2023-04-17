using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Properties;

public class CSIDiaphragm : Base
{
  [SchemaInfo("CSI Diaphragm", "Create an CSI Diaphragm", "CSI", "Properties")]
  public CSIDiaphragm(string name, bool semiRigid)
  {
    this.name = name;
    SemiRigid = semiRigid;
  }

  public CSIDiaphragm() { }

  public string name { get; set; }
  public bool SemiRigid { get; set; }
}

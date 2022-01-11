using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.ETABS.Properties
{
  public class ETABSDiaphragm : Base
  {
  public string name { get; set; }
  public bool SemiRigid { get; set; }

    [SchemaInfo("ETABS Diaphragm", "Create an ETABS Diaphragm", "ETABS", "Properties")]
    public ETABSDiaphragm(string name, bool semiRigid)
    {
      this.name = name;
      SemiRigid = semiRigid;
    }

    public ETABSDiaphragm()
    {
    }
  }
}

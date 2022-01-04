using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.ETABS.Properties;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
  void diaphragmToNative(ETABSDiaphragm etabsDiaphragm){
      Model.Diaphragm.SetDiaphragm(etabsDiaphragm.name, etabsDiaphragm.SemiRigid);

  }
  ETABSDiaphragm diaphragmToSpeckle(string name){
      bool semiRigid = false;
      Model.Diaphragm.GetDiaphragm(name, ref semiRigid);
      return new ETABSDiaphragm(name,semiRigid);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Properties;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    void diaphragmToNative(CSIDiaphragm CSIDiaphragm)
    {
      Model.Diaphragm.SetDiaphragm(CSIDiaphragm.name, CSIDiaphragm.SemiRigid);

    }
    CSIDiaphragm diaphragmToSpeckle(string name)
    {
      bool semiRigid = false;
      Model.Diaphragm.GetDiaphragm(name, ref semiRigid);
      return new CSIDiaphragm(name, semiRigid);
    }
  }
}
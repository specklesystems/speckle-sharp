using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;
using Objects.Structural.Geometry;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    private void DiaphragmToNative(CSIDiaphragm CSIDiaphragm, ref ApplicationObject appObj)
    {
      //TODO: test this bad boy, I'm not sure how it would create anything meaningful with just a name and a bool
      var success = Model.Diaphragm.SetDiaphragm(CSIDiaphragm.name, CSIDiaphragm.SemiRigid);

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: CSIDiaphragm.name);
      else
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Unable to create diaphragm with id {CSIDiaphragm.id}");
    }
    CSIDiaphragm diaphragmToSpeckle(string name)
    {
      bool semiRigid = false;
      Model.Diaphragm.GetDiaphragm(name, ref semiRigid);
      return new CSIDiaphragm(name, semiRigid);
    }
  }
}
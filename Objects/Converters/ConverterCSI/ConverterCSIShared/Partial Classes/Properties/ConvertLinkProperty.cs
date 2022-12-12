using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.CSI.Properties;
using System.Linq;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void LinkPropertyToNative(CSILinkProperty linkProperty, ref ApplicationObject appObj)
    {
      double[] value = new double[4];
      value[0] = linkProperty.M2PdeltaEnd1;
      value[1] = linkProperty.MP2deltaEnd2;
      value[2] = linkProperty.MP3deltaEnd1;
      value[3] = linkProperty.MP3deltaEnd2;

      // TODO: test if this works, because I don't think it will...
      var success1 = Model.PropLink.SetPDelta(linkProperty.name, ref value);
      var success2 = Model.PropLink.SetWeightAndMass(linkProperty.name, linkProperty.weight, linkProperty.mass, linkProperty.rotationalInertia1, linkProperty.rotationalInertia2, linkProperty.rotationalInertia3);

      if (success1 == 0 && success2 == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: $"{linkProperty.name}");
      else
        appObj.Update(status: ApplicationObject.State.Failed);
    }
    public CSILinkProperty LinkPropertyToSpeckle(string name)
    {
      double W = 0;
      double M = 0;
      double R1 = 0;
      double R2 = 0;
      double R3 = 0;
      double[] Value = null;
      Model.PropLink.GetWeightAndMass(name, ref W, ref M, ref R1, ref R2, ref R3);
      Model.PropLink.GetPDelta(name, ref Value);
      var speckleLinkProp = new CSILinkProperty(name, M, W, R1, R2, R3, Value[0], Value[1], Value[2], Value[3]);
      return speckleLinkProp;
    }
  }
}
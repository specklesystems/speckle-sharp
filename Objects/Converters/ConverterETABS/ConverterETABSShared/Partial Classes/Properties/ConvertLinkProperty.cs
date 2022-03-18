using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Structural.ETABS.Properties;
using System.Linq;


namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
    public void LinkPropertyToNative(ETABSLinkProperty linkProperty){
      double[] value = new double[4];
      value[0] = linkProperty.M2PdeltaEnd1;
      value[1] = linkProperty.MP2deltaEnd2;
      value[2] = linkProperty.MP3deltaEnd1;
      value[3] = linkProperty.MP3deltaEnd2;
      Model.PropLink.SetPDelta(linkProperty.name, ref value);
      Model.PropLink.SetWeightAndMass(linkProperty.name, linkProperty.weight, linkProperty.mass, linkProperty.rotationalInertia1, linkProperty.rotationalInertia2, linkProperty.rotationalInertia3);
      return;
    }
    public ETABSLinkProperty LinkPropertyToSpeckle(string name)
    {
      double W = 0;
      double M = 0;
      double R1 = 0;
      double R2 = 0;
      double R3 = 0;
      double[] Value = null;
      Model.PropLink.GetWeightAndMass(name, ref W, ref M, ref R1, ref R2, ref R3);
      Model.PropLink.GetPDelta(name, ref Value);
      var speckleLinkProp = new ETABSLinkProperty(name, M, W, R1, R2, R3, Value[0], Value[1], Value[2], Value[3]);
      return speckleLinkProp;
    }
  }
}

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
      SpeckleModel.properties.Add(speckleLinkProp);
      return speckleLinkProp;
    }
  }
}

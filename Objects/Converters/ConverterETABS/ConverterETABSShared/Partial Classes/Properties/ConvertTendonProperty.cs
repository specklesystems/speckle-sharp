using System;
using System.Collections.Generic;
using Objects.Structural.Properties.Profiles;
using ETABSv1;
using System.Linq;
using Objects.Structural.ETABS.Properties;
using Objects.Structural.Properties;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
  public ETABSTendonProperty TendonPropToSpeckle(string name){
      var specklePropertyTendon = new ETABSTendonProperty();
      string matProp = null;
      int modelingOption = 0;
      double area = 0;
      int color = 0;
      string notes = null;
      string GUID = null;
      Model.PropTendon.GetProp(name, ref matProp, ref modelingOption, ref area, ref color, ref notes, ref GUID);
      specklePropertyTendon.applicationId = GUID;
      specklePropertyTendon.Area = area;
      specklePropertyTendon.material = MaterialToSpeckle(matProp);
      if(modelingOption ==1){
        specklePropertyTendon.modelingOption = ModelingOption.Loads;
      }
      else{ specklePropertyTendon.modelingOption = ModelingOption.Elements; }
      return specklePropertyTendon;

  }

  }
}

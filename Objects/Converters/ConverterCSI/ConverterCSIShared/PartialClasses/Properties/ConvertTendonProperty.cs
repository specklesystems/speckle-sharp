using Objects.Structural.CSI.Properties;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public CSITendonProperty TendonPropToSpeckle(string name)
  {
    var specklePropertyTendon = new CSITendonProperty();
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
    if (modelingOption == 1)
    {
      specklePropertyTendon.modelingOption = ModelingOption.Loads;
    }
    else
    {
      specklePropertyTendon.modelingOption = ModelingOption.Elements;
    }
    return specklePropertyTendon;
  }
}

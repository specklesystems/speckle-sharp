using Objects.Structural.CSI.Properties;

namespace Objects.Converter.CSI
{
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
      string guid = null;
      Model.PropTendon.GetProp(name, ref matProp, ref modelingOption, ref area, ref color, ref notes, ref guid);
      specklePropertyTendon.applicationId = guid;
      specklePropertyTendon.Area = area;
      specklePropertyTendon.material = MaterialToSpeckle(matProp);
      specklePropertyTendon.modelingOption = modelingOption == 1 ? ModelingOption.Loads : ModelingOption.Elements;
      return specklePropertyTendon;
    }
  }
}

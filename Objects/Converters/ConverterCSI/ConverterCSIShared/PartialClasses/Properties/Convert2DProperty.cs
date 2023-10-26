using CSiAPIv1;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Properties;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    void SetProperties(CSIProperty2D property2D, string matProp, double thickness, string name)
    {
      property2D.name = name;
      property2D.thickness = thickness;
      property2D.material = MaterialToSpeckle(matProp);
    }

    private string Property2DToNative(CSIProperty2D property2D)
    {
      if (property2D.type2D == CSIPropertyType2D.Wall)
      {
        return WallPropertyToNative(property2D);
      }
      else
      {
        return FloorPropertyToNative(property2D);
      }
    }

    private CSIProperty2D? Property2DToSpeckle(string area, string property)
    {
      eAreaDesignOrientation areaDesignOrientation = eAreaDesignOrientation.Null;
      Model.AreaObj.GetDesignOrientation(area, ref areaDesignOrientation);

      return areaDesignOrientation switch
      {
        eAreaDesignOrientation.Wall => WallPropertyToSpeckle(property),
        eAreaDesignOrientation.Floor => FloorPropertyToSpeckle(property),
        _ => null
      };
    }
  }
}

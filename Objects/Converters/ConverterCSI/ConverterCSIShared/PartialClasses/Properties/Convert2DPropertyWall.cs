using CSiAPIv1;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public string WallPropertyToNative(CSIProperty2D wall)
    {
      throw new ConversionNotSupportedException("Wall properties are not currently supported on receive");
    }

    public CSIProperty2D WallPropertyToSpeckle(string property)
    {
      eWallPropType wallPropType = eWallPropType.Specified;
      eShellType shellType = eShellType.Layered;
      string matProp = "";
      double thickness = 0;
      int color = 0;
      string notes = "";
      string guid = "";

      Model.PropArea.GetWall(
        property,
        ref wallPropType,
        ref shellType,
        ref matProp,
        ref thickness,
        ref color,
        ref notes,
        ref guid
      );
      var speckleShellType = ConvertShellType(shellType);

      var speckleProperty2DWall = new CSIProperty2D
      {
        type = Structural.PropertyType2D.Wall,
        shellType = speckleShellType,
        type2D = Structural.CSI.Analysis.CSIPropertyType2D.Wall,
        applicationId = guid
      };

      SetProperties(speckleProperty2DWall, matProp, thickness, property);
      return speckleProperty2DWall;
    }
  }
}

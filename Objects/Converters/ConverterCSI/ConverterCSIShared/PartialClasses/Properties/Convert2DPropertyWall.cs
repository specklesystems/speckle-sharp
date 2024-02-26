using CSiAPIv1;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public string WallPropertyToNative(CSIProperty2D Wall)
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
    string GUID = "";
    var specklePropery2DWall = new CSIProperty2D();
    specklePropery2DWall.type = Structural.PropertyType2D.Wall;
    Model.PropArea.GetWall(
      property,
      ref wallPropType,
      ref shellType,
      ref matProp,
      ref thickness,
      ref color,
      ref notes,
      ref GUID
    );
    var speckleShellType = ConvertShellType(shellType);
    specklePropery2DWall.shellType = speckleShellType;
    setProperties(specklePropery2DWall, matProp, thickness, property);
    specklePropery2DWall.type2D = Structural.CSI.Analysis.CSIPropertyType2D.Wall;
    specklePropery2DWall.applicationId = GUID;
    return specklePropery2DWall;
  }
}

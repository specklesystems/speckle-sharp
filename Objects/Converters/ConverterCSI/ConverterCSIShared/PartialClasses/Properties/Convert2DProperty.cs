using CSiAPIv1;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Properties;
using Objects.Structural.Properties;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  void setProperties(CSIProperty2D property2D, string matProp, double thickeness, string name)
  {
    property2D.name = name;
    property2D.thickness = thickeness;
    property2D.material = MaterialToSpeckle(matProp);
    return;
  }

  private string Property2DToNative(Property2D property2D)
  {
    // Walls are typically shells (axially loaded)
    if (property2D.type == Structural.PropertyType2D.Wall || property2D.type == Structural.PropertyType2D.Shell)
    {
      return WallPropertyToNative(property2D);
    }
    // Floors are typically plates (loaded in bending and shear)
    else
    {
      return FloorPropertyToNative(property2D);
    }
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

  private CSIProperty2D Property2DToSpeckle(string area, string property)
  {
    eAreaDesignOrientation areaDesignOrientation = eAreaDesignOrientation.Null;
    Model.AreaObj.GetDesignOrientation(area, ref areaDesignOrientation);
    eDeckType deckType = eDeckType.Filled;
    eSlabType slabType = eSlabType.Drop;
    eWallPropType wallPropType = eWallPropType.Specified;
    eShellType shellType = eShellType.Layered;
    string matProp = "";
    double thickness = 0;
    int color = 0;
    string notes = "";
    string GUID = "";

    switch (areaDesignOrientation)
    {
      case eAreaDesignOrientation.Wall:
        return WallPropertyToSpeckle(property);
        break;
      case eAreaDesignOrientation.Floor:
        return FloorPropertyToSpeckle(property);
        break;
    }

    double[] value = null;

    return null;
  }
}

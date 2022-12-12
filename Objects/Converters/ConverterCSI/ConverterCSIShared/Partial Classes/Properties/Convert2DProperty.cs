using System;
using System.Collections.Generic;
using System.Text;
using CSiAPIv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {

    void setProperties(CSIProperty2D property2D, string matProp, double thickeness, string name)
    {
      property2D.name = name;
      property2D.thickness = thickeness;
      property2D.material = MaterialToSpeckle(matProp);
      return;
    }
    private void Property2DToNative(CSIProperty2D property2D, ref ApplicationObject appObj)
    {
      if (property2D.type2D == CSIPropertyType2D.Wall)
      {
        WallPropertyToNative(property2D, ref appObj);
      }
      else { FloorPropertyToNative(property2D, ref appObj); }
    }

    CSIProperty2D Property2DToSpeckle(string area, string property)
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
}
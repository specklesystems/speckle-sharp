using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Properties;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {

        void setProperties(ETABSProperty2D property2D, string matProp, double thickeness)
        {
            property2D.thickness = thickeness;
            property2D.material = MaterialToSpeckle(matProp);
            return;
        }
        object Property2DToNative(ETABSProperty2D property2D)
        {
            return property2D.name;
        }

        ETABSProperty2D Property2DToSpeckle(string area, string property)
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
                    var specklePropery2DWall = new ETABSProperty2D();
                    specklePropery2DWall.type = Structural.PropertyType2D.Wall;
                    Model.PropArea.GetWall(property, ref wallPropType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref GUID);
                    setProperties(specklePropery2DWall, matProp, thickness);
                    specklePropery2DWall.type2D = Structural.ETABS.Analysis.ETABSPropertyType2D.Wall;
                    return specklePropery2DWall;
                case eAreaDesignOrientation.Floor:
                    return ConvertFloor(property);
                    break;
            }

            double[] value = null;
            //Model.AreaObj.GetModifiers(area, ref value);
            //speckleProperty2D.modifierInPlane = value[2];
            //speckleProperty2D.modifierBending = value[5];
            //speckleProperty2D.modifierShear = value[6];
            return null;
        }

    }
}

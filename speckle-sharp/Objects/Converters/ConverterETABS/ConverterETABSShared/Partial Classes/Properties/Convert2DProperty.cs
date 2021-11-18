using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.ETABS.Properties;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {

        void setProperties(ETABSProperty2D property2D, string matProp, double thickeness, string name)
        {
            property2D.name = name;
            property2D.thickness = thickeness;
            property2D.material = MaterialToSpeckle(matProp);
            return;
        }
        object Property2DToNative(ETABSProperty2D property2D)
        {
            if (property2D.type2D == ETABSPropertyType2D.Wall)
            {
                WallPropertyToNative(property2D);
            }
            else { FloorPropertyToNative(property2D); }
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
                    return WallPropertyToSpeckle(property);
                    break;
                case eAreaDesignOrientation.Floor:
                    return FloorPropertyToSpeckle(property);
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
